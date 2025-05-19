using Librarr.Data;
using Librarr.Model;
using Librarr.Settings;
using Librarr.Utils;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Services.Jobs;

/// <summary>
/// Checks the download status of torrents and imports the files to the library folder if necessary
/// </summary>
public class LibraryImportJob(
    SettingsService settingsService,
    SnackMessageBus snackBus,
    ILogger<LibraryImportJob> logger) : IJob
{
    public TimeSpan Interval => TimeSpan.FromSeconds(5);

    public async Task ExecuteAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var db = scope.ServiceProvider.GetRequiredService<LibrarrDbContext>();

        var profileSettings = settingsService.GetSettings<ProfileSettingsData>();
        var librarySettings = settingsService.GetSettings<LibrarySettingsData>();

        if (librarySettings.LibraryPath == null)
        {
            logger.LogDebug("No library path configured, skipping import");
            return;
        }

        await foreach (var libraryFile in db.Files
                           .Where(b => b.Status == LibraryFile.DownloadStatus.Downloaded)
                           .Include(lf => lf.Book)
                           .ThenInclude(b => b.Author)
                           .AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            try
            {
                TryImportTorrent(libraryFile, profileSettings.GetProfile(libraryFile.Type), librarySettings, db);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to import torrent {Hash} from {File}", libraryFile.TorrentHash,
                    libraryFile.SourcePath);
            }
        }
    }

    private void TryImportTorrent(LibraryFile libraryFile, ProfileSettingsData.Profile profile,
        LibrarySettingsData librarySettings, LibrarrDbContext db)
    {
        // snackBus.ShowInfo($"Importing release: {libraryFile.TorrentHash} for book: {libraryFile.Book.Title}");
        logger.LogInformation("Importing torrent: {Hash} for {BookTitle}", libraryFile.TorrentHash,
            libraryFile.Book.Title);

        var contentPath = libraryFile.SourcePath!;
        var isDirectory = Directory.Exists(contentPath);
        var isFile = File.Exists(contentPath);

        if (!isDirectory && !isFile)
        {
            logger.LogError("Path does not exist: {Path}", contentPath);
            throw new Exception($"Path does not exist: {contentPath}");
        }

        // Find file
        string[]? sourceFiles = null;
        string? sourceFormat = null;
        if (isDirectory)
        {
            foreach (var format in profile.Formats.Where(f => f.Enabled))
            {
                var files = Directory.GetFiles(contentPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith($".{format.Name}", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (files.Length > 0)
                {
                    sourceFiles = files;
                    sourceFormat = format.Name;
                    break;
                }
            }
        }
        else
        {
            sourceFiles = [contentPath];
            foreach (var format in profile.Formats.Where(f => f.Enabled))
            {
                if (contentPath.EndsWith(format.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    sourceFormat = format.Name;
                    break;
                }
            }
        }

        if (sourceFormat == null || sourceFiles == null)
        {
            logger.LogError("Could not find file for book: {Title} with format: {Format}, source: {Path}",
                libraryFile.Book.Title, sourceFormat, contentPath);
            throw new Exception(
                $"Could not find file for book: {libraryFile.Book.Title} with format: {sourceFormat}, source: {contentPath}");
        }

        logger.LogInformation("Using source files: {Files}", string.Join(',', sourceFiles));

        // Import book
        // TODO: Support hardlinks
        var authorFolderName = FileUtils.SanitizePathName(libraryFile.Book.Author.Name);
        var bookFolderName = FileUtils.SanitizePathName(libraryFile.Book.Title);

        var destinationDir =
            Directory.CreateDirectory(Path.Combine(librarySettings.LibraryPath!, authorFolderName,
                bookFolderName)); // TODO: Verify?
        var destinationFiles = new List<string>(sourceFiles.Length);

        foreach (var sourceFile in sourceFiles)
        {
            var destinationFile = Path.Combine(destinationDir.FullName, Path.GetFileName(sourceFile));
            if (librarySettings.CreateHardLinks)
            {
                FileUtils.CreateHardLink(sourceFile, destinationFile);
            }
            else
            {
                File.Copy(sourceFile, destinationFile, true);
            }

            destinationFiles.Add(destinationFile);
        }

        libraryFile.DestinationFiles = destinationFiles;
        libraryFile.Format = sourceFormat;

        // Mark torrent as imported
        libraryFile.Status = LibraryFile.DownloadStatus.Imported;

        db.Files.Update(libraryFile);

        snackBus.ShowInfo($"Imported book: {libraryFile.Book.Title} to {destinationDir.FullName}");
        logger.LogInformation("Imported {Book} to {Directory}", libraryFile.Book.Title, destinationDir.FullName);
    }
}
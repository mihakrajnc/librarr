using System.Collections.Frozen;
using Librarr.Data;
using Librarr.Model;
using Librarr.Services.Download;
using Librarr.Settings;
using Librarr.Utils;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Services;

public class LibraryImportBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<LibraryImportBackgroundService> logger,
    SettingsService settingsService,
    IDownloadService dlService,
    SnackMessageBus snackBus) : BackgroundService
{
    private const int POOL_DELAY = 5000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetService<LibrarrDbContext>()!;

            await UpdateTorrents(db);

            await db.SaveChangesAsync(stoppingToken);

            await CheckTorrents(db);

            await db.SaveChangesAsync(stoppingToken);

            await Task.Delay(POOL_DELAY, stoppingToken);
        }
    }

    private async Task UpdateTorrents(LibrarrDbContext db)
    {
        // We only update the status of non-imported torrents
        var filesToCheck = db.Files
            .Where(t => t.Status == LibraryFile.DownloadStatus.Downloading ||
                        t.Status == LibraryFile.DownloadStatus.Pending)
            .Where(t => t.TorrentHash != null);
        var hashes = filesToCheck.Select(t => t.TorrentHash).OfType<string>();

        if (!await hashes.AnyAsync()) return;

        logger.LogInformation("Querying QBT for torrent status");

        var torrents = (await dlService.GetTorrents(hashes)).ToFrozenDictionary(t => t.Hash);

        logger.LogInformation($"QBT responded with {torrents.Count} torrents");

        await foreach (var file in filesToCheck.AsAsyncEnumerable())
        {
            if (torrents.TryGetValue(file.TorrentHash!, out var torrentItem))
            {
                file.SourcePath = torrentItem.Path;
                var newStatus = torrentItem.DownloadCompleted
                    ? LibraryFile.DownloadStatus.Downloaded
                    : LibraryFile.DownloadStatus.Downloading;
                if (file.Status != newStatus)
                {
                    file.Status = newStatus;
                    logger.LogInformation(
                        $"Updated torrent status to {file.Status}: {file.TorrentHash}");
                }
            }
            else
            {
                // Torrent was deleted from download client
                file.Status = LibraryFile.DownloadStatus.Failed; // TODO: Should we have a separate Missing status?
                logger.LogWarning($"Missing torrent: {file.TorrentHash}");
            }

            db.Files.Update(file);
        }
    }

    private async Task CheckTorrents(LibrarrDbContext db)
    {
        var profileSettings = settingsService.GetSettings<ProfileSettingsData>();
        var librarySettings = settingsService.GetSettings<LibrarySettingsData>();

        if (librarySettings.LibraryPath == null)
        {
            logger.LogInformation("No library path configured, skipping import");
            return;
        }

        await foreach (var libraryFile in db.Files
                           .Where(b => b.Status == LibraryFile.DownloadStatus.Downloaded)
                           .Include(lf => lf.Book)
                           .ThenInclude(b => b.Author)
                           .AsAsyncEnumerable())
        {
            try
            {
                TryImportTorrent(libraryFile, profileSettings.GetProfile(libraryFile.Type), librarySettings, db);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to import ebook torrent: {libraryFile.Book.Title}");
            }
        }
    }

    private void TryImportTorrent(LibraryFile libraryFile,
        ProfileSettingsData.Profile profile, LibrarySettingsData librarySettings, LibrarrDbContext db)
    {
        snackBus.ShowInfo($"Importing release: {libraryFile.TorrentHash} for book: {libraryFile.Book.Title}");
        logger.LogInformation($"Importing release: {libraryFile.TorrentHash} for book: {libraryFile.Book.Title}");

        var contentPath = libraryFile.SourcePath!;
        var isDirectory = Directory.Exists(contentPath);
        var isFile = File.Exists(contentPath);

        if (!isDirectory && !isFile)
        {
            logger.LogError($"Path does not exist: {contentPath}");
            throw new Exception($"Path does not exist: {contentPath}");
        }

        // Find file
        string[]? sourceFiles = null;
        string? sourceFormat = null;
        if (isDirectory)
        {
            foreach (var format in profile.Formats.Where(f => f.Enabled))
            {
                var files = Directory.GetFiles(contentPath, $"*.{format.Name}", SearchOption.AllDirectories);
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
            logger.LogError(
                $"Could not find file for book: {libraryFile.Book.Title} with format: {sourceFormat}, source: {contentPath}");
            throw new Exception(
                $"Could not find file for book: {libraryFile.Book.Title} with format: {sourceFormat}, source: {contentPath}");
        }

        logger.LogInformation($"Using source files: {string.Join(',', sourceFiles)}");

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
            File.Copy(sourceFile, destinationFile, true);
            destinationFiles.Add(destinationFile);
        }

        libraryFile.DestinationFiles = destinationFiles;
        libraryFile.Format = sourceFormat;

        // Mark torrent as imported
        libraryFile.Status = LibraryFile.DownloadStatus.Imported;

        db.Files.Update(libraryFile);

        snackBus.ShowInfo($"Imported book: {libraryFile.Book.Title} to {destinationDir.FullName}");
        logger.LogInformation($"Imported book: {libraryFile.Book.Title} to {destinationDir.FullName}");
    }
}
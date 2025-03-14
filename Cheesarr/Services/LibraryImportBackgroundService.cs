using Cheesarr.Data;
using Cheesarr.Model;
using Cheesarr.Settings;
using Cheesarr.Utils;
using Microsoft.EntityFrameworkCore;

namespace Cheesarr.Services;

public class LibraryImportBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<LibraryImportBackgroundService> logger,
    SettingsService settingsService,
    SnackMessageBus snackBus) : BackgroundService
{
    private const int POOL_DELAY = 5000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckTorrents();

            await Task.Delay(POOL_DELAY, stoppingToken);
        }
    }

    private async Task CheckTorrents()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetService<CheesarrDbContext>()!;
        var profileSettings = settingsService.GetSettings<ProfileSettingsData>();

        foreach (var book in db.Books.Where(b =>
                         b.EBookTorrent != null && b.EBookTorrent.TorrentStatus == TorrentEntry.Status.Downloaded)
                     .Include(be => be.EBookTorrent)
                     .Include(be => be.Author))
        {
            try
            {
                TryImportTorrent(book, book.EBookTorrent!, true, profileSettings.EBookProfile, db);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to import ebook torrent: {book.Title}");
            }
        }

        foreach (var book in db.Books.Where(b =>
                         b.AudiobookTorrent != null &&
                         b.AudiobookTorrent.TorrentStatus == TorrentEntry.Status.Downloaded)
                     .Include(be => be.AudiobookTorrent)
                     .Include(be => be.Author))
        {
            try
            {
                TryImportTorrent(book, book.AudiobookTorrent!, true, profileSettings.AudiobookProfile,db);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to import ebook torrent: {book.Title}");
            }
        }
        
        await db.SaveChangesAsync();
    }

    private void TryImportTorrent(BookEntry book, TorrentEntry torrent, bool isEbook, ProfileSettingsData.Profile profile, CheesarrDbContext db)
    {
        snackBus.ShowInfo($"Importing release: {torrent.Hash} for book: {book.Title}");
        logger.LogInformation($"Importing torrent {torrent.Hash} for book: {book.Title}");

        var contentPath = torrent.ContentPath;
        var isDirectory = Directory.Exists(contentPath);
        var isFile = File.Exists(contentPath);
        
        var librarySettings = settingsService.GetSettings<LibrarySettingsData>();

        if (!isDirectory && !isFile)
        {
            logger.LogError($"Path does not exist: {contentPath}");
            throw new Exception($"Path does not exist: {contentPath}");
        }

        // Find file
        string? sourceFile = null;
        string? sourceFormat = null;
        if (isDirectory)
        {
            foreach (var format in profile.Formats.Where(f => f.Enabled))
            {
                sourceFile = Directory.GetFiles(contentPath, $"*.{format.Name}").FirstOrDefault();
                sourceFormat = format.Name;
                if (sourceFile != null) break;
            }
        }
        else
        {
            // TODO: Not great
            sourceFile = contentPath;
            foreach (var format in profile.Formats.Where(f => f.Enabled))
            {
                sourceFormat = format.Name;
                if (sourceFile != null) break;
            }
        }

        if (sourceFile == null || sourceFormat == null)
        {
            logger.LogError(
                $"Could not find file for book: {book.Title} with format: {sourceFormat}, source: {sourceFile}");
            throw new Exception($"Could not find file for book: {book.Title}");
        }

        logger.LogInformation($"Using source file: {sourceFile}");

        // Import book
        // TODO: Support hardlinks
        var authorFolderName = FileUtils.SanitizePathName(book.Author.Name);
        var bookFolderName = FileUtils.SanitizePathName(book.Title);

        var destinationDir =
            Directory.CreateDirectory(Path.Combine(librarySettings.LibraryPath, authorFolderName,
                bookFolderName)); // TODO: Verify?
        var destinationFile = Path.Combine(destinationDir.FullName, Path.GetFileName(sourceFile));

        File.Copy(sourceFile, destinationFile, true); // TODO: Should we overwrite?

        // Add the file to the db and book entry
        if (isEbook)
        {
            book.EBookFile = new FileEntry
            {
                Path = destinationFile,
                Format = sourceFormat,
            };
        }
        else
        {
            book.AudiobookFile = new FileEntry
            {
                Path = destinationFile,
                Format = sourceFormat,
            };
        }

        // Mark torrent as imported
        torrent.TorrentStatus = TorrentEntry.Status.Imported;

        db.Books.Update(book);
        db.Torrents.Update(torrent);

        snackBus.ShowInfo($"Imported book: {book.Title} to {destinationFile}");
        logger.LogInformation($"Imported book: {book.Title} to {destinationFile}");
    }
}
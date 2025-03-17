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
        var hashes = db.Torrents.Where(t => !t.IsImported).Select(t => t.Hash);

        if (!hashes.Any()) return;

        logger.LogInformation("Querying QBT for torrent status");

        var torrents = (await dlService.GetTorrents(hashes)).ToDictionary(t => t.Hash);

        logger.LogInformation($"QBT responded with {torrents.Count} torrents");

        foreach (var torrentEntry in db.Torrents.Where(t => !t.IsImported))
        {
            if (torrents.TryGetValue(torrentEntry.Hash, out var torrentItem))
            {
                torrentEntry.ContentPath = torrentItem.Path;
                var newStatus = torrentItem.DownloadCompleted
                    ? TorrentEntry.Status.Downloaded
                    : TorrentEntry.Status.Downloading;
                if (torrentEntry.TorrentStatus != newStatus)
                {
                    torrentEntry.TorrentStatus = newStatus;
                    logger.LogInformation(
                        $"Updated torrent status to {torrentEntry.TorrentStatus}: {torrentEntry.Hash}");
                }
            }
            else
            {
                // Torrent was deleted from download client
                torrentEntry.TorrentStatus = TorrentEntry.Status.Missing;
                logger.LogWarning($"Missing torrent: {torrentEntry.Hash}");
            }

            db.Torrents.Update(torrentEntry);
        }
    }

    private async Task CheckTorrents(LibrarrDbContext db)
    {
        var profileSettings = settingsService.GetSettings<ProfileSettingsData>();

        foreach (var book in db.Books
                     .Where(b => b.EBookTorrent != null &&
                                 b.EBookTorrent.TorrentStatus == TorrentEntry.Status.Downloaded &&
                                 !b.EBookTorrent.IsImported)
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

        foreach (var book in db.Books
                     .Where(b => b.AudiobookTorrent != null &&
                                 b.AudiobookTorrent.TorrentStatus == TorrentEntry.Status.Downloaded &&
                                 !b.AudiobookTorrent.IsImported)
                     .Include(be => be.AudiobookTorrent)
                     .Include(be => be.Author))
        {
            try
            {
                TryImportTorrent(book, book.AudiobookTorrent!, false, profileSettings.AudiobookProfile, db);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to import ebook torrent: {book.Title}");
            }
        }
    }

    private void TryImportTorrent(BookEntry book, TorrentEntry torrent, bool isEbook,
        ProfileSettingsData.Profile profile, LibrarrDbContext db)
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
                $"Could not find file for book: {book.Title} with format: {sourceFormat}, source: {contentPath}");
            throw new Exception($"Could not find file for book: {book.Title}");
        }

        logger.LogInformation($"Using source files: {string.Join(',', sourceFiles)}");

        // Import book
        // TODO: Support hardlinks
        var authorFolderName = FileUtils.SanitizePathName(book.Author.Name);
        var bookFolderName = FileUtils.SanitizePathName(book.Title);

        var destinationDir =
            Directory.CreateDirectory(Path.Combine(librarySettings.LibraryPath, authorFolderName,
                bookFolderName)); // TODO: Verify?

        foreach (var sourceFile in sourceFiles)
        {
            var destinationFile = Path.Combine(destinationDir.FullName, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destinationFile, true);
        }

        // Add the file to the db and book entry
        if (isEbook) // TODO: Clean this up
        {
            book.EBookFile = new FileEntry
            {
                Paths = sourceFiles,
                Format = sourceFormat,
            };
        }
        else
        {
            book.AudiobookFile = new FileEntry
            {
                Paths = sourceFiles,
                Format = sourceFormat,
            };
        }

        // Mark torrent as imported
        torrent.IsImported = true;

        db.Books.Update(book);
        db.Torrents.Update(torrent);

        snackBus.ShowInfo($"Imported book: {book.Title} to {destinationDir.FullName}");
        logger.LogInformation($"Imported book: {book.Title} to {destinationDir.FullName}");
    }
}
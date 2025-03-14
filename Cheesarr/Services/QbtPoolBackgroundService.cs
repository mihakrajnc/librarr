using Cheesarr.Data;
using Cheesarr.Model;
using Cheesarr.Settings;
using Cheesarr.Utils;

namespace Cheesarr.Services;

public class QbtPoolBackgroundService(
    QBTService qbtService,
    IServiceScopeFactory scopeFactory,
    SettingsService settingsService,
    ILogger<QbtPoolBackgroundService> logger) : BackgroundService
{
    private const int POOL_DELAY = 5000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunUpdate();

            await Task.Delay(POOL_DELAY, stoppingToken);
        }
    }

    private async Task RunUpdate()
    {
        logger.LogInformation("Querying QBT for torrent status");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetService<CheesarrDbContext>()!;

        var hashes = db.Torrents.Where(t =>
                t.TorrentStatus != TorrentEntry.Status.Downloaded && t.TorrentStatus != TorrentEntry.Status.Imported)
            .Select(t => t.Hash);

        if (!hashes.Any()) return;
        
        var qbtTorrents = await qbtService.GetTorrents(hashes);

        if (qbtTorrents.Length == 0)
        {
            logger.LogInformation("No matching torrents found");
            return;
        }

        logger.LogInformation($"Found {qbtTorrents.Length} matching torrents");

        foreach (var torrentInfo in qbtTorrents)
        {
            var isDownloaded = IsDownloaded(torrentInfo);
            var torrentEntry = db.Torrents.First(t => t.Hash == torrentInfo.hash);

            torrentEntry.TorrentStatus = isDownloaded
                ? TorrentEntry.Status.Downloaded
                : TorrentEntry.Status.Downloading;
            torrentEntry.ContentPath = torrentInfo.content_path;

            db.Torrents.Update(torrentEntry);
            
            logger.LogInformation($"Set torrent status to {torrentEntry.TorrentStatus}: {torrentEntry.Hash}");

            //
            // bookEntry.Status = torrentInfo.completion_on != -1 // TODO: Probably need to check against other values?
            //     ? Status.Downloaded
            //     : Status.Downloading;
            //
            // if (bookEntry.Status == Status.Downloaded)
            // {
            //     try
            //     {
            //         await ImportEbook(bookEntry, torrentInfo.content_path, db);
            //         bookEntry.Status = Status.Imported;
            //     }
            //     catch (Exception e)
            //     {
            //         logger.LogError(e, $"Failed to import book: {bookEntry.Title}");
            //     }
            // }
            //
            // db.Books.Update(bookEntry);
            //
            // logger.LogInformation($"Set book status to {bookEntry.Status}: {bookEntry.Title}");
        }

        await db.SaveChangesAsync();
    }

    private bool IsDownloaded(QBTTorrentInfoResponse torrentInfo)
    {
        return torrentInfo.state is TorrentState.PausedUp or TorrentState.Uploading or TorrentState.StalledUp
            or TorrentState.QueuedUp or TorrentState.ForcedUp;
    }

    // TODO: Move to it's own service
    // private async Task ImportEbook(BookEntry book, string path, CheesarrDbContext db)
    // {
    //     logger.LogInformation($"Importing book: {book.Title}");
    //
    //     var isDirectory = Directory.Exists(path);
    //     var isFile = File.Exists(path);
    //     var profileSettings = settingsService.GetSettings<ProfileSettingsData>();
    //     var librarySettings = settingsService.GetSettings<LibrarySettingsData>();
    //
    //     if (!isDirectory && !isFile)
    //     {
    //         logger.LogError($"Path does not exist: {path}");
    //         throw new Exception($"Path does not exist: {path}");
    //     }
    //
    //     // Find file
    //     string? sourceFile = null;
    //     string? sourceFormat = null;
    //     if (isDirectory)
    //     {
    //         foreach (var format in profileSettings.EBookProfile.Formats.Where(f => f.Enabled))
    //         {
    //             sourceFile = Directory.GetFiles(path, $"*.{format.Name}").FirstOrDefault();
    //             sourceFormat = format.Name;
    //             if (sourceFile != null) break;
    //         }
    //     }
    //     else
    //     {
    //         // TODO: Not great
    //         sourceFile = path;
    //         foreach (var format in profileSettings.EBookProfile.Formats.Where(f => f.Enabled))
    //         {
    //             sourceFormat = format.Name;
    //             if (sourceFile != null) break;
    //         }
    //     }
    //
    //     if (sourceFile == null || sourceFormat == null)
    //     {
    //         logger.LogError(
    //             $"Could not find file for book: {book.Title} with format: {sourceFormat}, source: {sourceFile}");
    //         throw new Exception($"Could not find file for book: {book.Title}");
    //     }
    //
    //     logger.LogInformation($"Using source file: {sourceFile}");
    //
    //     // Import book
    //     // TODO: Support hardlinks
    //     var authorFolderName = FileUtils.SanitizePathName(book.Author.Name);
    //     var bookFolderName = FileUtils.SanitizePathName(book.Title);
    //
    //     var destinationDir =
    //         Directory.CreateDirectory(Path.Combine(librarySettings.LibraryPath, authorFolderName,
    //             bookFolderName)); // TODO: Verify?
    //     var destinationFile = Path.Combine(destinationDir.FullName, Path.GetFileName(sourceFile));
    //
    //     File.Copy(sourceFile, destinationFile, true); // TODO: Should we overwrite?
    //
    //     // Add the file to the db and book entry
    //     var fileEntry = (await db.Files.AddAsync(new FileEntry
    //     {
    //         Path = destinationFile,
    //         Format = sourceFormat
    //     })).Entity;
    //     book.Files.Add(fileEntry);
    //
    //     logger.LogInformation($"Imported book: {book.Title} to {destinationFile}");
    // }
}
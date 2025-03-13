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

        var hashes =
            db.Books.Select(be => be.EBookTorrentHash)
                .Where(th => !string.IsNullOrEmpty(th)); // TODO: Check for already completed
        var torrents = await qbtService.GetTorrents(hashes);

        if (torrents.Length == 0)
        {
            logger.LogInformation("No matching torrents found");
            return;
        }

        logger.LogInformation($"Found {torrents.Length} matching torrents");

        foreach (var torrentInfo in torrents)
        {
            var bookEntry = db.Books.FirstOrDefault(be => be.EBookTorrentHash == torrentInfo.hash);
            if (bookEntry == null)
            {
                logger.LogWarning($"Found no matching torrent: {torrentInfo.hash}");
                continue;
            }

            // TODO: Clean this up
            if (bookEntry.Status is Status.Imported) continue;

            bookEntry.Status = torrentInfo.completion_on != -1 // TODO: Probably need to check against other values?
                ? Status.Downloaded
                : Status.Downloading;

            if (bookEntry.Status == Status.Downloaded)
            {
                try
                {
                    await ImportEbook(bookEntry, torrentInfo.content_path, db);
                    bookEntry.Status = Status.Imported;
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Failed to import book: {bookEntry.Title}");
                }
            }

            db.Books.Update(bookEntry);

            logger.LogInformation($"Set book status to {bookEntry.Status}: {bookEntry.Title}");
        }

        await db.SaveChangesAsync();
    }

    // TODO: Move to it's own service
    private async Task ImportEbook(BookEntry book, string path, CheesarrDbContext db)
    {
        logger.LogInformation($"Importing book: {book.Title}");

        var isDirectory = Directory.Exists(path);
        var isFile = File.Exists(path);
        var profileSettings = settingsService.GetSettings<ProfileSettingsData>();
        var librarySettings = settingsService.GetSettings<LibrarySettingsData>();

        if (!isDirectory && !isFile)
        {
            logger.LogError($"Path does not exist: {path}");
            throw new Exception($"Path does not exist: {path}");
        }

        // Find file
        string? sourceFile = null;
        string? sourceFormat = null;
        if (isDirectory)
        {
            foreach (var format in profileSettings.EBookProfile.Formats.Where(f => f.Enabled))
            {
                sourceFile = Directory.GetFiles(path, $"*.{format.Name}").FirstOrDefault();
                sourceFormat = format.Name;
                if (sourceFile != null) break;
            }
        }
        else
        {
            // TODO: Not great
            sourceFile = path;
            foreach (var format in profileSettings.EBookProfile.Formats.Where(f => f.Enabled))
            {
                sourceFormat = format.Name;
                if (sourceFile != null) break;
            }
        }

        if (sourceFile == null || sourceFormat == null)
        {
            logger.LogError($"Could not find file for book: {book.Title} with format: {sourceFormat}, source: {sourceFile}");
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
        var fileEntry = (await db.Files.AddAsync(new FileEntry
        {
            Path = destinationFile,
            Format = sourceFormat
        })).Entity;
        book.Files.Add(fileEntry);

        logger.LogInformation($"Imported book: {book.Title} to {destinationFile}");
    }
}
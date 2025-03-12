using Cheesarr.Data;
using Cheesarr.Model;

namespace Cheesarr.Services;

public class QbtPoolBackgroundService(
    QBTService qbtService,
    IServiceScopeFactory scopeFactory,
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

            if (bookEntry.Status is Status.Imported or Status.Downloaded) continue;

            bookEntry.Status = torrentInfo.completion_on != -1 // TODO: Probably need to check against other values?
                ? Status.Downloaded
                : Status.Downloading;
            db.Books.Update(bookEntry);

            logger.LogInformation($"Set book status to {bookEntry.Status}: {bookEntry.Title}");
        }

        await db.SaveChangesAsync();
    }
}

public class GrabStateUpdater
{
    public event EventHandler<int>? NumberChanged;

    public void OnNumberUpdated(int number)
    {
        NumberChanged?.Invoke(this, number);
    }
}
using Cheesarr.Data;
using Cheesarr.Model;
using Cheesarr.Services.Download;

namespace Cheesarr.Services;

public class DownloadStatusBackgroundService(
    IDownloadService dlService,
    IServiceScopeFactory scopeFactory,
    ILogger<DownloadStatusBackgroundService> logger) : BackgroundService
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
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetService<CheesarrDbContext>()!;

        var hashes = db.Torrents.Where(t =>
                t.TorrentStatus != TorrentEntry.Status.Downloaded && t.TorrentStatus != TorrentEntry.Status.Imported)
            .Select(t => t.Hash);

        if (!hashes.Any()) return;

        logger.LogInformation("Querying QBT for torrent status");

        var torrents = await dlService.GetTorrents(hashes);

        if (torrents.Length == 0)
        {
            logger.LogInformation("No matching torrents found");
            return;
        }

        logger.LogInformation($"Found {torrents.Length} matching torrents");

        foreach (var item in torrents)
        {
            var torrentEntry = db.Torrents.First(t => t.Hash == item.Hash);

            torrentEntry.TorrentStatus = item.Status == TorrentItem.DownloadStatus.Downloaded
                ? TorrentEntry.Status.Downloaded
                : TorrentEntry.Status.Downloading;
            torrentEntry.ContentPath = item.Path;

            db.Torrents.Update(torrentEntry);

            logger.LogInformation($"Set torrent status to {torrentEntry.TorrentStatus}: {torrentEntry.Hash}");
        }

        await db.SaveChangesAsync();
    }
}
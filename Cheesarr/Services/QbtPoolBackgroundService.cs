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
        }

        await db.SaveChangesAsync();
    }

    private bool IsDownloaded(QBTTorrentInfoResponse torrentInfo)
    {
        return torrentInfo.state is QBTTorrentInfoResponse.State.PausedUp or QBTTorrentInfoResponse.State.Uploading
            or QBTTorrentInfoResponse.State.StalledUp
            or QBTTorrentInfoResponse.State.QueuedUp or QBTTorrentInfoResponse.State.ForcedUp;
    }
}
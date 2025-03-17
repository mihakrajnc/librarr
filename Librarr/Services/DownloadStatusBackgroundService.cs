using Librarr.Data;
using Librarr.Model;
using Librarr.Services.Download;

namespace Librarr.Services;

public class DownloadStatusBackgroundService(
    IDownloadService dlService,
    IServiceScopeFactory scopeFactory,
    ILogger<DownloadStatusBackgroundService> logger) : BackgroundService
{
    private const int POOL_DELAY = 5000;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // while (!ct.IsCancellationRequested)
        // {
        //     using var scope = scopeFactory.CreateScope();
        //     var db = scope.ServiceProvider.GetService<LibrarrDbContext>()!;
        //
        //     await UpdateTorrents(db);
        //
        //     await db.SaveChangesAsync(ct);
        //
        //     await Task.Delay(POOL_DELAY, ct);
        // }
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
}
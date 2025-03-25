using System.Collections.Frozen;
using Librarr.Data;
using Librarr.Model;
using Librarr.Services.Download;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Services.Jobs;

/// <summary>
/// Queries the download service for the status of tracked torrents and updates the database entries.
/// </summary>
public class UpdateTorrentsStatusJob(IDownloadService dlService, ILogger<UpdateTorrentsStatusJob> logger) : IJob
{
    public TimeSpan Interval => TimeSpan.FromSeconds(20);

    public async Task ExecuteAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var db = scope.ServiceProvider.GetRequiredService<LibrarrDbContext>();

        // We only update the status of non-imported torrents
        var filesToCheck = db.Files
            .Where(t => t.Status == LibraryFile.DownloadStatus.Downloading ||
                        t.Status == LibraryFile.DownloadStatus.Pending)
            .Where(t => t.TorrentHash != null);
        var hashes = filesToCheck.Select(t => t.TorrentHash).OfType<string>();

        if (!await hashes.AnyAsync(cancellationToken)) return;

        logger.LogInformation("Querying QBT for torrent status");

        var torrents = (await dlService.GetTorrents(hashes)).ToFrozenDictionary(t => t.Hash);

        logger.LogInformation($"QBT responded with {torrents.Count} torrents");

        await foreach (var file in filesToCheck.AsAsyncEnumerable().WithCancellation(cancellationToken))
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

        await db.SaveChangesAsync(cancellationToken);
    }
}
using System.Text.Json.Serialization;

namespace Librarr.Services.Download;

// ReSharper disable InconsistentNaming
[Serializable]
public record QBTTorrentItemResponse(
    string content_path,
    string hash,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    QBTTorrentItemResponse.State state
    // int added_on,
    // int amount_left,
    // bool auto_tmm,
    // float availability,
    // string category,
    // string comment,
    // int completed,
    // int completion_on,
    // int dl_limit,
    // int dlspeed,
    // string download_path,
    // int downloaded,
    // int downloaded_session,
    // int eta,
    // bool f_l_piece_prio,
    // bool force_start,
    // bool has_metadata,
    // int inactive_seeding_time_limit,
    // string infohash_v1,
    // string infohash_v2,
    // int last_activity,
    // string magnet_uri,
    // int max_inactive_seeding_time,
    // int max_ratio,
    // int max_seeding_time,
    // string name,
    // int num_complete,
    // int num_incomplete,
    // int num_leechs,
    // int num_seeds,
    // int popularity,
    // int priority,
    // bool @private,
    // float progress,
    // int ratio,
    // int ratio_limit,
    // int reannounce,
    // string root_path,
    // string save_path,
    // int seeding_time,
    // int seeding_time_limit,
    // int seen_complete,
    // bool seq_dl,
    // int size,
    // bool super_seeding,
    // string tags,
    // int time_active,
    // int total_size,
    // string tracker,
    // int trackers_count,
    // int up_limit,
    // int uploaded,
    // int uploaded_session,
    // int upspeed
)
{
    public enum State
    {
        Error, //Some error occurred, applies to paused torrents
        MissingFiles, //Torrent data files is missing
        Uploading, //Torrent is being seeded and data is being transferred
        PausedUp, //Torrent is paused and has finished downloading
        QueuedUp, //Queuing is enabled and torrent is queued for upload
        StalledUp, //Torrent is being seeded, but no connection were made
        CheckingUp, //Torrent has finished downloading and is being checked
        ForcedUp, //Torrent is forced to uploading and ignore queue limit
        Allocating, //Torrent is allocating disk space for download
        Downloading, //Torrent is being downloaded and data is being transferred
        MetaDl, //Torrent has just started downloading and is fetching metadata
        PausedDl, //Torrent is paused and has NOT finished downloading
        QueuedDl, //Queuing is enabled and torrent is queued for download
        StalledDl, //Torrent is being downloaded, but no connection were made
        CheckingDl, //Same as checkingUP, but torrent has NOT finished downloading
        ForcedDl, //Torrent is forced to downloading to ignore queue limit
        CheckingResumeData, //Checking resume data on qBt startup
        Moving, //Torrent is moving to another location
        Unknown, //Unknown status
    }

    public TorrentItem ToTorrentItem()
    {
        var isDownloaded =
            state is State.PausedUp or State.Uploading or State.StalledUp or State.QueuedUp or State.ForcedUp;

        return new TorrentItem(
            content_path,
            hash,
            isDownloaded
        );
    }
}
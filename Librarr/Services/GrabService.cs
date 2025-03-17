using Librarr.Data;
using Librarr.Model;
using Librarr.Services.Download;
using Librarr.Services.ReleaseSearch;
using Librarr.Settings;

namespace Librarr.Services;

public class GrabService(
    IReleaseSearchService releaseSearchService,
    SettingsService settingsService,
    ILogger<GrabService> logger,
    IDownloadService dlService,
    IServiceScopeFactory scopeFactory,
    SnackMessageBus snackBus)
{
    public async Task SearchForBook(BookEntry book)
    {
        logger.LogInformation($"Searching for {book.Title}");
        snackBus.ShowInfo($"Searching for {book.Title}");

        var searchTerm = book.Title; // + " by " + book.Author.Name;
        var ebookWanted = book.EBookWanted;
        var audiobooksWanted = book.AudiobookWanted;
        var profileSettings = settingsService.GetSettings<ProfileSettingsData>();

        var prowlarrResponse = await releaseSearchService.Search(searchTerm, ebookWanted, audiobooksWanted);
        var parsedItems = prowlarrResponse.Select(ParsedItem.Create).ToList();

        logger.LogInformation($"Found {parsedItems.Count} items");

        if (ebookWanted)
        {
            var ebookHash = await PickAndGrabRelease(book, parsedItems, profileSettings.EBookProfile);
            if (ebookHash != null)
            {
                book.EBookTorrent = new TorrentEntry
                {
                    Hash = ebookHash
                };
            }
        }

        if (audiobooksWanted)
        {
            var audiobookHash = await PickAndGrabRelease(book, parsedItems, profileSettings.AudiobookProfile);
            if (audiobookHash != null)
            {
                book.AudiobookTorrent = new TorrentEntry
                {
                    Hash = audiobookHash
                };
            }
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibrarrDbContext>();

        db.Books.Update(book);

        await db.SaveChangesAsync();
    }

    private async Task<string?> PickAndGrabRelease(BookEntry book, List<ParsedItem> parsedItems,
        ProfileSettingsData.Profile profile)
    {
        var formatsSet = profile.Formats.Where(f => f.Enabled).Select(f => f.Name).ToHashSet();
        var matchingItems = parsedItems.Where(pi => formatsSet.Intersect(pi.Formats).Any()).OrderBy(pi => pi.Seeders)
            .ToList();

        logger.LogInformation($"Kept {matchingItems.Count} ebook items");

        ParsedItem? selectedItem = null;
        foreach (var format in profile.Formats.Where(f => f.Enabled))
        {
            selectedItem = matchingItems.FirstOrDefault(pi => pi.Formats.Contains(format.Name));
            if (selectedItem != null) break;
        }

        if (selectedItem != null)
        {
            logger.LogInformation($"Selected release: {selectedItem.Title}");
            snackBus.ShowInfo($"Selected release {selectedItem.Title}");

            var hash = await GrabItem(selectedItem);

            if (hash == null)
            {
                logger.LogError("Failed to download torrent");
                return null;
            }

            snackBus.ShowInfo($"Release grabbed: {selectedItem.Title}");

            return hash;
        }

        snackBus.ShowInfo($"No matches found for: {book.Title}");

        return null;
    }

    private async Task<string?> GrabItem(ParsedItem item)
    {
        var (data, hash) = await releaseSearchService.DownloadTorrentFile(item.DownloadURL);
        await dlService.AddTorrent(data, hash);

        return hash;
    }

    private class ParsedItem
    {
        public required string Title;
        public required string Language;
        public int Seeders;
        public int Leechers;
        public required string DownloadURL;
        public bool VIP;
        public required HashSet<string> Formats;

        public static ParsedItem Create(ReleaseSearchItem rsi)
        {
            var firstBracket = rsi.Title.IndexOf('[');
            var secondBracket = rsi.Title.IndexOf(']', firstBracket + 1);

            var tags = rsi.Title.Substring(firstBracket + 1,
                    secondBracket - firstBracket - 1)
                .Split(" / ");

            return new ParsedItem
            {
                Title = rsi.Title,
                Language = tags[0],
                Formats = [..tags.Skip(1)],
                Seeders = rsi.Seeders,
                Leechers = rsi.Leechers,
                DownloadURL = rsi.DownloadURL,
                VIP = rsi.Title.EndsWith("[VIP]")
            };
        }
    }
}
using Cheesarr.Data;
using Cheesarr.Model;
using Cheesarr.Settings;

namespace Cheesarr.Services;

public class GrabService(ProwlarrService prowlarr, SettingsService settingsService, ILogger<GrabService> logger, QBTService qbtService, IServiceScopeFactory ssfactory)
{
    public async Task SearchForBook(BookEntry book)
    {
        if (book.GrabType is GrabType.Audiobook or GrabType.Both)
        {
            throw new NotSupportedException("Audiobooks are not supported yet");
        }
        
        logger.LogInformation($"Searching for {book.GrabType}");
        
        var searchTerm = book.Title;// + " by " + book.Author.Name;
        var ebooks = book.GrabType is GrabType.EBook or GrabType.Both;
        var audiobooks = book.GrabType is GrabType.Audiobook or GrabType.Both;

        var profileSettings = settingsService.GetSettings<ProfileSettingsData>();
        var audiobookFormats = profileSettings.AudiobookProfile.Formats
            .Where(f => f.Enabled)
            .Select(f => f.Name)
            .ToHashSet();
        var ebookFormats = profileSettings.EBookProfile.Formats
            .Where(f => f.Enabled)
            .Select(f => f.Name)
            .ToHashSet();
        
        
        var prowlarrResponse = await prowlarr.Search(searchTerm, ebooks, audiobooks);
        var parsedItems = prowlarrResponse.Select(ParsedItem.Create).ToList();
        
        logger.LogInformation($"Found {parsedItems.Count} items");
        
        var ebookItems = parsedItems.Where(pi => ebookFormats.Intersect(pi.Formats).Any()).OrderBy(pi => pi.Seeders).ToList();
        var audiobookItems = parsedItems.Where(pi => audiobookFormats.Intersect(pi.Formats).Any()).OrderBy(pi => pi.Seeders).ToList();

        logger.LogInformation($"Kept {ebookItems.Count} ebook items");
        logger.LogInformation($"Kept {audiobookItems.Count} audiobook items");

        ParsedItem selectedEbookItem = null;
        
        foreach (var format in profileSettings.EBookProfile.Formats)
        {
            if (!format.Enabled) continue; // TODO do better
            
            var foundItem = ebookItems.FirstOrDefault(pi => pi.Formats.Contains(format.Name));

            if (foundItem != null)
            {
                selectedEbookItem = foundItem;
                break;
            }
        }
        
        // TODO Audio books

        if (selectedEbookItem != null)
        {
            logger.LogInformation($"Selected ebook release: {selectedEbookItem.Title}");
            var hash = await DownloadItem(selectedEbookItem);
            
            if (hash == null)
            {
                logger.LogError("Failed to download torrent");
                return;
            }


            using var scope = ssfactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CheesarrDbContext>();

            book.EBookTorrentHash = hash;
            db.Books.Update(book);
        }
    }
    
    private Task<string?> DownloadItem(ParsedItem item)
    {
        return qbtService.DownloadTorrent(item.DownloadURL);
    }

    private class ParsedItem
    {
        public string Title;
        public string Language;
        public int Seeders;
        public int Leechers;
        public string DownloadURL;
        public HashSet<string> Formats;

        public static ParsedItem Create(ProwlarrItem pi)
        {
            var tags = pi.title.Substring(pi.title.LastIndexOf('[') + 1,
                pi.title.LastIndexOf(']') - pi.title.LastIndexOf('[') - 1)
                .Split(" / ");
            
            return new ParsedItem
            {
                Title = pi.title,
                Language = tags[0],
                Formats = new HashSet<string>(tags.Skip(1)),
                Seeders = pi.seeders,
                Leechers = pi.leechers,
                DownloadURL = pi.downloadUrl,
            };
        }
    }

}
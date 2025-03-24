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
    public async Task SearchForBook(Book book)
    {
        logger.LogInformation($"Searching for {book.Title}");
        snackBus.ShowInfo($"Searching for {book.Title}");

        var searchTerm = book.Title; // + " by " + book.Author.Name;
        var ebookWanted = book.EBookWanted;
        var audiobooksWanted = book.AudiobookWanted;
        var profileSettings = settingsService.GetSettings<ProfileSettingsData>();

        // TODO: Maybe make two searches, one for ebook one for audiobook
        var searchItems = await releaseSearchService.Search(searchTerm, ebookWanted, audiobooksWanted);

        logger.LogInformation($"Found {searchItems.Length} items");

        if (ebookWanted)
        {
            var hash = await PickAndGrabRelease(book, searchItems, profileSettings.EBookProfile,
                profileSettings.Language);
            if (hash != null)
            {
                book.Files.Add(new LibraryFile
                {
                    Book = book,
                    Status = LibraryFile.DownloadStatus.Pending,
                    TorrentHash = hash,
                    Type = LibraryFile.FileType.Ebook
                });
            }
        }

        if (audiobooksWanted)
        {
            var hash = await PickAndGrabRelease(book, searchItems, profileSettings.AudiobookProfile,
                profileSettings.Language);
            if (hash != null)
            {
                book.Files.Add(new LibraryFile
                {
                    Book = book,
                    Status = LibraryFile.DownloadStatus.Pending,
                    TorrentHash = hash,
                    Type = LibraryFile.FileType.Audiobook
                });
            }
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibrarrDbContext>();

        db.Books.Update(book);

        await db.SaveChangesAsync();
    }
    
    public async Task<string?> GrabItem(ReleaseSearchItem item)
    {
        var (data, hash) = await releaseSearchService.DownloadTorrentFile(item.DownloadURL);
        await dlService.AddTorrent(data, hash);

        return hash;
    }

    private async Task<string?> PickAndGrabRelease(Book book, ReleaseSearchItem[] searchItems,
        ProfileSettingsData.Profile profile, string language)
    {
        var formatsSet = profile.Formats.Where(f => f.Enabled).Select(f => f.Name).ToHashSet();

        // Filter only the formats that we want
        var formatMatches = searchItems.Where(pi => formatsSet.Intersect(pi.Formats).Any());

        // Filter only the language we want
        var languageMatches = formatMatches.Where(rsi => rsi.Language == language);

        // Filter only titles that contain all the words from the original title TODO: This could be better
        var titleMatches = languageMatches.Where(pi =>
            book.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .All(word => pi.Title.Contains(word, StringComparison.OrdinalIgnoreCase)));

        // TODO: Filter by min seeders
        var seedersOrdered = titleMatches.OrderBy(pi => pi.Seeders).ToList();

        var finalSet = seedersOrdered;

        logger.LogInformation($"Kept {finalSet.Count} ebook items");

        ReleaseSearchItem? selectedItem = null;
        foreach (var format in profile.Formats.Where(f => f.Enabled))
        {
            // If multiple releases match pick the one with the most downloads
            selectedItem = finalSet.Where(pi => pi.Formats.Contains(format.Name))
                .OrderByDescending(rsi => rsi.Downloads).FirstOrDefault();
            if (selectedItem != null) break;
        }

        if (selectedItem != null)
        {
            logger.LogInformation($"Selected release: {selectedItem.Title}");
            // snackBus.ShowInfo($"Selected release {selectedItem.Title}");

            var hash = await GrabItem(selectedItem);

            if (hash == null)
            {
                logger.LogError("Failed to download torrent");
                return null;
            }

            // snackBus.ShowInfo($"Release grabbed: {selectedItem.Title}");

            return hash;
        }

        // snackBus.ShowInfo($"No matches found for: {book.Title}");

        return null;
    }
}
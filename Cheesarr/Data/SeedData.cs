using Cheesarr.Model;

namespace Cheesarr.Data;

public static class SeedData
{
    public static void Initialize(CheesarrDbContext context)
    {
        context.Profiles.Add(new ProfileEntry
        {
            Name = "EBook",
            Formats = [BookFormat.AZW3, BookFormat.EPUB, BookFormat.MOBI],
        });
        
        context.Profiles.Add(new ProfileEntry
        {
            Name = "AudioBook",
            Formats = [BookFormat.FLAC, BookFormat.M4B, BookFormat.MP3],
        });

        context.SaveChanges();
    }
}
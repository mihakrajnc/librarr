using Cheesarr.Model;

namespace Cheesarr.Data;

public static class SeedData
{
    public static void Initialize(CheesarrDbContext context)
    {
        context.Books.AddRange(
            new BookEntry
            {
                Id = 0,
                Title = "Les Misérables"
            },
            new BookEntry
            {
                Id = 2,
                Title = "The Secret Garden"
            },
            new BookEntry
            {
                Id = 3,
                Title = "The Hobbit"
            },
            new BookEntry
            {
                Id = 4,
                Title = "The Great Gatsby"
            },
            new BookEntry
            {
                Id = 5,
                Title = "To Kill a Mockingbird"
            }
        );
    }
}
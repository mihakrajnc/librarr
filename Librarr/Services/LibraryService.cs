using Librarr.Data;
using Librarr.Model;
using Librarr.Services.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Services;

public class LibraryService(LibrarrDbContext db, ILogger<LibraryService> logger)
{
    public async Task<List<Book>> GetBooks()
    {
        logger.LogInformation("Retrieving all books");
        var books = await db.Books
            .Include(b => b.Author)
            .Include(b => b.Files)
            .ToListAsync();

        logger.LogInformation("Retrieved {Count} books", books.Count);

        return books;
    }

    public async Task<Book?> GetBook(int id)
    {
        logger.LogInformation("Retrieving book with id {BookId}", id);
        var book = await db.Books.FindAsync(id);
        logger.LogInformation("Book {BookId} was found: {Found}", id, book != null);

        return book;
    }

    public async Task<Book> AddBook(BookSearchItem bookItem, bool ebook, bool audiobook)
    {
        logger.LogInformation("Adding a new book with OLID {BookOlid} and title {BookTitle}", bookItem.ID,
            bookItem.Title);
        var authorName = bookItem.AuthorName;
        var authorKey = bookItem.AuthorID;

        var author = db.Authors.FirstOrDefault(a => a.OLID == authorKey);
        if (author == null)
        {
            author = db.Authors.Add(new Author
            {
                OLID = authorKey,
                Name = authorName
            }).Entity;
            logger.LogInformation("New author added with OLID {AuthorOLID} and name {AuthorName}", authorKey, authorName);
        }

        var bookEntry = new Book
        {
            OLID = bookItem.ID,
            Title = bookItem.Title,
            Author = author,
            FirstPublishYear = bookItem.PublishYear,
            EBookWanted = ebook,
            AudiobookWanted = audiobook,
            CoverURL = bookItem.CoverURL
        };

        db.Books.Add(bookEntry);

        await db.SaveChangesAsync();
        logger.LogInformation("Book with OLID {BookOlid} added successfully", bookItem.ID);
        return bookEntry;
    }

    public bool BookExists(string olid)
    {
        logger.LogInformation("Checking if book with OLID {BookOlid} exists", olid);
        bool exists = db.Books.Any(e => e.OLID == olid);
        logger.LogInformation("Book with OLID {BookOlid} exists: {Exists}", olid, exists);
        return exists;
    }

    public async Task<bool> DeleteBook(int id)
    {
        logger.LogInformation("Attempting to delete book with id {BookId}", id);
        var book = await db.Books.FindAsync(id);

        if (book == null)
        {
            logger.LogWarning("Book with id {BookId} not found", id);
            return false;
        }

        db.Books.Remove(book);
        await db.SaveChangesAsync();
        logger.LogInformation("Book with id {BookId} deleted successfully", id);
        return true;
    }

    public async Task<List<Author>> GetAuthors()
    {
        logger.LogInformation("Retrieving all authors");
        var authors = await db.Authors
            .Include(a => a.Books)
            .ToListAsync();
        logger.LogInformation("Retrieved {Count} authors", authors.Count);

        return authors;
    }

    public Task<Author?> GetAuthor(int id)
    {
        logger.LogInformation("Retrieving author with id {AuthorId}", id);
        return db.Authors.FindAsync(id).AsTask();
    }
}
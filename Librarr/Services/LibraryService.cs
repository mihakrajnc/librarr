using Librarr.Data;
using Librarr.Model;
using Librarr.Services.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Librarr.Services;

public class LibraryService(LibrarrDbContext db)
{
    public Task<List<Book>> GetBooks()
    {
        return db.Books
            .Include(b => b.Author)
            .Include(b => b.Files)
            .ToListAsync();
    }

    public Task<Book?> GetBook(int id)
    {
        return db.Books.FindAsync(id).AsTask();
    }

    public async Task<Book> AddBook(BookSearchItem bookItem, bool ebook, bool audiobook)
    {
        var authorName = bookItem.AuthorName;
        var authorKey = bookItem.AuthorID;

        var author = db.Authors.FirstOrDefault(a => a.OLID == authorKey) ?? db.Authors.Add(new Author
        {
            OLID = authorKey,
            Name = authorName
        }).Entity;

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

        return bookEntry;
    }

    public bool BookExists(string olid)
    {
        return db.Books.Any(e => e.OLID == olid);
    }

    public async Task<bool> DeleteBook(int id)
    {
        var book = await db.Books.FindAsync(id);

        if (book == null)
        {
            return false;
        }

        db.Books.Remove(book);
        await db.SaveChangesAsync();
        return true;
    }

    public Task<List<Author>> GetAuthors()
    {
        return db.Authors
            .Include(a => a.Books)
            .ToListAsync();
    }

    public Task<Author?> GetAuthor(int id)
    {
        return db.Authors.FindAsync(id).AsTask();
    }
}
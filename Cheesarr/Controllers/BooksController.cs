// using Cheesarr.Data;
// using Cheesarr.Model;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
//
// namespace Cheesarr.Controllers;
//
// [ApiController, Route("books")]
// public class BooksController(CheesarrDbContext db) : Controller
// {
//     [HttpGet]
//     public async Task<ActionResult<List<BookEntry>>> GetBooks()
//     {
//         return (await db.Books.ToListAsync()).ToList();
//     }
//     
//     [HttpPost]
//     public async Task<IActionResult> AddBook([FromBody] BookEntry book)
//     {
//         if (book == null || string.IsNullOrWhiteSpace(book.Title))
//             return BadRequest("Invalid book data");
//
//         db.Books.Add(book);
//         await db.SaveChangesAsync();
//
//         return CreatedAtAction(nameof(GetBooks), new { id = book.Id }, book);
//     }
// }
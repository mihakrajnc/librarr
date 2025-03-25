using Librarr.Services;
using Microsoft.AspNetCore.Mvc;

namespace Librarr.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(LibraryService library) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBooks()
    {
        return Ok(await library.GetBooks());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var success = await library.DeleteBook(id);
        return success ? NoContent() : BadRequest();
    }
}
using Librarr.Services;
using Microsoft.AspNetCore.Mvc;

namespace Librarr.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController(LibraryService library) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAuthors()
    {
        var result = await library.GetAuthors();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAuthor(int id)
    {
        var author = await library.GetAuthor(id);

        if (author == null)
            return NotFound();

        return Ok(author);
    }
}
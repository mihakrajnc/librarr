using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Librarr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        /// <summary>
        /// Login endpoint that validates user credentials and issues an authentication cookie.
        /// </summary>
        /// <param name="loginRequest">The login credentials.</param>
        /// <returns>A redirect on success or a BadRequest result on failure.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromForm] LoginRequest loginRequest)
        {
            // Retrieve credentials from environment variables (or use defaults)
            var envUsername = Environment.GetEnvironmentVariable("APP_USERNAME") ?? "admin";
            var envPassword = Environment.GetEnvironmentVariable("APP_PASSWORD") ?? "password";

            if (loginRequest.Username == envUsername && loginRequest.Password == envPassword)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, loginRequest.Username)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "LibrarrAuth");
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync("LibrarrAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

                // Redirect to the home page after a successful login
                return Redirect("/");
            }

            return BadRequest("Invalid credentials");
        }
        
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("LibrarrAuth");
            // Redirect to login page after logout
            return Redirect("/login");
        }
    }

    // Simple record for login data
    public record LoginRequest(string Username, string Password);
}
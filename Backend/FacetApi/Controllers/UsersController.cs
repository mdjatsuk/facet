using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FacetApi.Data.Repos;
using FacetApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace FacetApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UsersRepo _repo;

        public UsersController(UsersRepo repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] User login)
        {
            var token = await _repo.Login(login);

            if (token == "__BANNED__")
            {
                // Return 403 with a helpful message so frontend can show 'banned' notice
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Your account has been banned. To appeal, contact support." });
            }

            if (!string.IsNullOrEmpty(token))
            {
                return Ok(token);
            }

            return Unauthorized(new { message = "Invalid username or password" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var (success, error) = await _repo.Register(user);
            if (success)
            {
                return Ok();
            }

            if (error == "Username already exists")
            {
                return Conflict(new { message = error });
            }

            return BadRequest(new { message = error });
        }

        // Admin-only endpoint to change a user's role (moved under 'manage')
        [Authorize(Roles = "Admin")]
        [HttpPost("manage/set-role")]
        public async Task<IActionResult> SetRole([FromBody] SetRoleRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Role))
                return BadRequest(new { message = "Username and Role are required" });

            var updated = await _repo.SetUserRole(req.Username, req.Role);
            if (!updated) return NotFound(new { message = "User not found" });

            return Ok();
        }

        // Admin-only endpoint to ban/unban a user
        [Authorize(Roles = "Admin")]
        [HttpPost("manage/ban")]
        public async Task<IActionResult> Ban([FromBody] BanRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username))
                return BadRequest(new { message = "Username is required" });

            var updated = await _repo.BanUser(req.Username, req.Ban);
            if (!updated) return NotFound(new { message = "User not found" });

            return Ok();
        }

        // Admin-only endpoint to delete a user account
        [Authorize(Roles = "Admin")]
        [HttpDelete("manage/{username}")]
        public async Task<IActionResult> DeleteUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return BadRequest(new { message = "Username is required" });
            var deleted = await _repo.DeleteUser(username);
            if (!deleted) return NotFound(new { message = "User not found" });
            return Ok();
        }

        // Admin-only endpoint to get user info (no sensitive fields)
        [Authorize(Roles = "Admin")]
        [HttpGet("manage/{username}")]
        public async Task<IActionResult> GetUserInfo(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return BadRequest(new { message = "Username is required" });
            var user = await _repo.GetByUsername(username);
            if (user == null) return NotFound(new { message = "User not found" });

            // Return safe user info only
            var info = new {
                id = user.Id,
                username = user.Username,
                role = user.Role,
                isBanned = user.IsBanned
            };

            return Ok(info);
        }
    }

}

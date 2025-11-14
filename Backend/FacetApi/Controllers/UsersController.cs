using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FacetApi.Data.Repos;
using FacetApi.Models;

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

            if (!string.IsNullOrEmpty(token))
            {
                return Ok(token);
            }
            else
            {
                return Unauthorized();
            }
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

    }
}

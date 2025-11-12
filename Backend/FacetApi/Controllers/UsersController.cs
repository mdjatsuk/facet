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

    }
}

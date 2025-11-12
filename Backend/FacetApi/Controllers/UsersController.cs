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
            if (login == null)
                return BadRequest("Login payload is required.");

            var isLoginSuccessful = await _repo.Login(login);

            return isLoginSuccessful ? Ok() : Unauthorized();
        }
    }
}

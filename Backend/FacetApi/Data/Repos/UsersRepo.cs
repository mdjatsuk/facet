using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FacetApi.Models;
using DocumentFormat.OpenXml.Spreadsheet;

namespace FacetApi.Data.Repos
{
    public class UsersRepo
    {
        private readonly FacetDbContext _context;
        private IConfiguration _config;


        public UsersRepo(IConfiguration config, FacetDbContext context)
        {
            _config = config;
            _context = context;
        }

        public async Task<bool> Login([FromBody] User login)
        {
            var dbUser = await _context.UserList!.FirstOrDefaultAsync(user => user.Username == login.Username);

            return dbUser != null && dbUser.Password == HashPassword(login.Password);
        }

        private string HashPassword(string password)
        {
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: [],
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hashed;
        }

    }
}

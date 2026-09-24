using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FacetApi.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;


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

        public async Task<string> Login([FromBody] User login)
        {
            var dbUser = await _context.UserList!.FirstOrDefaultAsync(user => user.Username == login.Username);

            if (dbUser == null)
            {
                return "";
            }

            // If user is banned, return a sentinel value so controller can return a 403 with message
            if (dbUser.IsBanned)
            {
                return "__BANNED__";
            }

            // Check if password matches (support both plaintext legacy and hashed passwords)
            bool passwordValid = false;
            string? hashedPassword = null;

            // Try plaintext comparison (legacy support)
            if (dbUser.Password == login.Password)
            {
                passwordValid = true;
                // Hash the password for future logins (migration)
                hashedPassword = HashPassword(login.Password);
            }
            else
            {
                // Try hashed comparison (new standard)
                var hashed = HashPassword(login.Password, dbUser.Salt);
                if (dbUser.Password == hashed)
                {
                    passwordValid = true;
                }
            }

            if (!passwordValid)
            {
                return "";
            }

            // If password was plaintext and we hashed it, update the database (migrate to hashed)
            if (hashedPassword != null && string.IsNullOrEmpty(dbUser.Salt))
            {
                byte[] saltBytes = new byte[128 / 8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltBytes);
                }
                dbUser.Salt = Convert.ToBase64String(saltBytes);
                dbUser.Password = HashPassword(login.Password, dbUser.Salt);
                _context.UserList!.Update(dbUser);
                await _context.SaveChangesAsync();
            }

            return GenerateJSONWebToken(dbUser);

        }

        private string HashPassword(string password, string? salt = null)
        {
            byte[] saltBytes;
            if (string.IsNullOrEmpty(salt))
            {
                saltBytes = new byte[128 / 8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltBytes);
                }
            }
            else
            {
                saltBytes = Convert.FromBase64String(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hashed;
        }

        public async Task<(bool success, string? error)> Register([FromBody] User newUser)
        {
            // basic validation
            if (string.IsNullOrWhiteSpace(newUser.Username)) return (false, "Username is required");
            if (newUser.Username.Length < 3) return (false, "Username must be at least 3 characters");
            // disallow usernames that are only digits (e.g., "12345")
            if (newUser.Username.All(char.IsDigit)) return (false, "Username cannot be only digits");
            if (string.IsNullOrEmpty(newUser.Password) || newUser.Password.Length < 5) return (false, "Password must be at least 5 characters");
            // require at least one letter and one digit
            if (!newUser.Password.Any(char.IsLetter) || !newUser.Password.Any(char.IsDigit)) return (false, "Password must contain at least one letter and one digit");

            // check exists
            var existing = await _context.UserList!.FirstOrDefaultAsync(u => u.Username == newUser.Username);
            if (existing != null) return (false, "Username already exists");

            // create salt and hash
            byte[] saltBytes = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            var salt = Convert.ToBase64String(saltBytes);
            var hashed = HashPassword(newUser.Password, salt);

            newUser.Password = hashed;
            newUser.Salt = salt;
            // Never accept a role supplied by an unauthenticated registration request.
            newUser.Role = "User";

            _context.UserList!.Add(newUser);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> SetUserRole(string username, string role)
        {
            if (_context.UserList == null) return false;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(role)) return false;
            // normalize role
            role = role.Trim();
            var user = await _context.UserList.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;
            user.Role = role;
            _context.UserList.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BanUser(string username, bool ban)
        {
            if (_context.UserList == null) return false;
            if (string.IsNullOrWhiteSpace(username)) return false;
            var user = await _context.UserList.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;
            user.IsBanned = ban;
            _context.UserList.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUser(string username)
        {
            if (_context.UserList == null) return false;
            if (string.IsNullOrWhiteSpace(username)) return false;
            var user = await _context.UserList.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return false;
            _context.UserList.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetByUsername(string username)
        {
            if (_context.UserList == null) return null;
            if (string.IsNullOrWhiteSpace(username)) return null;
            return await _context.UserList.FirstOrDefaultAsync(u => u.Username == username);
        }

        private string GenerateJSONWebToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username)
                ,
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role ?? "User")
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

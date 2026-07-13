using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBackend.Data;
using MyBackend.DTOs;
using MyBackend.Models;
using MyBackend.Services;
using System;
using System.Threading.Tasks;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public AuthController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupDto signupDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var emailNormalized = signupDto.Email.ToLowerInvariant();
            var existingUser = await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == emailNormalized);
            if (existingUser)
            {
                return BadRequest(new { message = "Email is already registered." });
            }

            var (hash, salt) = PasswordHasher.HashPassword(signupDto.Password);
            var user = new User
            {
                Username = signupDto.Username,
                Email = signupDto.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var session = new UserSession
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.UserSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            var userDto = new UserDto(user.Id, user.Username, user.Email, user.CreatedAt);
            return Ok(new AuthResponseDto(token, userDto));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var emailNormalized = loginDto.Email.ToLowerInvariant();
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);
            if (user == null || !PasswordHasher.VerifyPassword(loginDto.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest(new { message = "Invalid email or password." });
            }

            var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var session = new UserSession
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.UserSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            var userDto = new UserDto(user.Id, user.Username, user.Email, user.CreatedAt);
            return Ok(new AuthResponseDto(token, userDto));
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "Missing or invalid token" });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

            if (session == null)
            {
                return Unauthorized(new { message = "Session expired or invalid" });
            }

            var user = await _dbContext.Users.FindAsync(session.UserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userDto = new UserDto(user.Id, user.Username, user.Email, user.CreatedAt);
            return Ok(userDto);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var session = await _dbContext.UserSessions.FirstOrDefaultAsync(s => s.Token == token);
                if (session != null)
                {
                    _dbContext.UserSessions.Remove(session);
                    await _dbContext.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Logged out successfully" });
        }
    }
}

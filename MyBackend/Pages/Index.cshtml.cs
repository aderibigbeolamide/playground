using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyBackend.Data;
using MyBackend.Models;
using MyBackend.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyBackend.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _dbContext;

        public IndexModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [BindProperty]
        public LoginInputModel LoginInput { get; set; } = new();

        [BindProperty]
        public SignupInputModel SignupInput { get; set; } = new();

        [BindProperty]
        public string ActiveTab { get; set; } = "login";

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Dashboard");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            ActiveTab = "login";
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var emailNormalized = LoginInput.Email.ToLowerInvariant();
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized);
            if (user == null || !PasswordHasher.VerifyPassword(LoginInput.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            // Create DB session log for statistics
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

            // Sign in using cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("SessionToken", token)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            
            TempData["SuccessMessage"] = "Logged in successfully!";
            return RedirectToPage("/Dashboard");
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            ActiveTab = "signup";
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (SignupInput.Password != SignupInput.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return Page();
            }

            var emailNormalized = SignupInput.Email.ToLowerInvariant();
            var existingUser = await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == emailNormalized);
            if (existingUser)
            {
                ModelState.AddModelError(string.Empty, "Email is already registered.");
                return Page();
            }

            var (hash, salt) = PasswordHasher.HashPassword(SignupInput.Password);
            var user = new User
            {
                Username = SignupInput.Username,
                Email = SignupInput.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Create DB session log for statistics
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

            // Sign in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("SessionToken", token)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            TempData["SuccessMessage"] = "Account registered and logged in successfully!";
            return RedirectToPage("/Dashboard");
        }
    }

    public class LoginInputModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class SignupInputModel
    {
        [Required(ErrorMessage = "Username is required")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

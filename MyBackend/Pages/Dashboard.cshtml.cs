using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyBackend.Data;
using MyBackend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyBackend.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _dbContext;

        public DashboardModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int UserId { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsApproved { get; set; }

        public IList<User> Users { get; set; } = new List<User>();
        public IList<MyBackend.Models.Blog> Blogs { get; set; } = new List<MyBackend.Models.Blog>();

        public async Task<IActionResult> OnGetAsync()
        {
            Username = User.Identity?.Name ?? "Guest";
            Email = User.FindFirstValue(ClaimTypes.Email) ?? "user@example.com";
            IsAdmin = User.FindFirstValue(ClaimTypes.Role) == "Admin";
            
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                UserId = userId;
                var user = await _dbContext.Users.FindAsync(userId);
                IsApproved = user?.IsApproved ?? false;

                if (IsAdmin)
                {
                    Users = await _dbContext.Users.ToListAsync();
                    Blogs = await _dbContext.Blogs.Include(b => b.Author).OrderByDescending(b => b.CreatedAt).ToListAsync();
                }
                else
                {
                    Blogs = await _dbContext.Blogs.Where(b => b.AuthorId == userId).OrderByDescending(b => b.CreatedAt).ToListAsync();
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostApproveUserAsync(int id)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin") return Forbid();
            var user = await _dbContext.Users.FindAsync(id);
            if (user != null)
            {
                user.IsApproved = true;
                await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "User approved.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeactivateUserAsync(int id)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin") return Forbid();
            var user = await _dbContext.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "User deactivated.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateUserAsync(int id)
        {
            if (User.FindFirstValue(ClaimTypes.Role) != "Admin") return Forbid();
            var user = await _dbContext.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = true;
                await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "User activated.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteBlogAsync(int id)
        {
            var blog = await _dbContext.Blogs.FindAsync(id);
            if (blog != null)
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool isAdmin = User.FindFirstValue(ClaimTypes.Role) == "Admin";
                
                if (isAdmin || (int.TryParse(userIdStr, out int userId) && blog.AuthorId == userId))
                {
                    _dbContext.Blogs.Remove(blog);
                    await _dbContext.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Blog deleted.";
                }
                else
                {
                    return Forbid();
                }
            }
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }
    }
}

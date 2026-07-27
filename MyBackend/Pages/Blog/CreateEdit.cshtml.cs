using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyBackend.Data;
using MyBackend.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyBackend.Pages.Blog
{
    [Authorize]
    public class CreateEditModel : PageModel
    {
        private readonly AppDbContext _dbContext;

        public CreateEditModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [BindProperty]
        public MyBackend.Models.Blog Blog { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                var blog = await _dbContext.Blogs.FindAsync(id.Value);
                if (blog == null) return NotFound();
                
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool isAdmin = User.FindFirstValue(ClaimTypes.Role) == "Admin";
                
                if (!isAdmin && (!int.TryParse(userIdStr, out int userId) || blog.AuthorId != userId))
                {
                    return Forbid();
                }

                Blog = blog;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Blog.Author");
            if (!ModelState.IsValid) return Page();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            bool isAdmin = User.FindFirstValue(ClaimTypes.Role) == "Admin";
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null || (!isAdmin && !user.IsApproved))
            {
                return Forbid();
            }

            if (Blog.Id == 0)
            {
                // Create
                Blog.AuthorId = userId;
                Blog.CreatedAt = DateTime.UtcNow;
                Blog.UpdatedAt = DateTime.UtcNow;
                _dbContext.Blogs.Add(Blog);
                TempData["SuccessMessage"] = "Blog created successfully!";
            }
            else
            {
                // Update
                var existingBlog = await _dbContext.Blogs.FindAsync(Blog.Id);
                if (existingBlog == null) return NotFound();

                if (!isAdmin && existingBlog.AuthorId != userId) return Forbid();

                existingBlog.Title = Blog.Title;
                existingBlog.Content = Blog.Content;
                existingBlog.UpdatedAt = DateTime.UtcNow;
                TempData["SuccessMessage"] = "Blog updated successfully!";
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToPage("/Dashboard");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyBackend.Data;
using MyBackend.Models;
using System.Threading.Tasks;

namespace MyBackend.Pages.Blog
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _dbContext;

        public DetailsModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public MyBackend.Models.Blog Blog { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var blog = await _dbContext.Blogs
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);
                
            if (blog == null)
            {
                return NotFound();
            }

            Blog = blog;
            return Page();
        }
    }
}

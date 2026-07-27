using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyBackend.Data;
using MyBackend.Models;
using System.Collections.Generic;
using System.Linq;
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

        public IList<MyBackend.Models.Blog> Blogs { get; set; } = new List<MyBackend.Models.Blog>();

        public async Task<IActionResult> OnGetAsync()
        {
            Blogs = await _dbContext.Blogs
                .Include(b => b.Author)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Page();
        }
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyBackend.Data;
using System.Threading.Tasks;

namespace MyBackend.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly AppDbContext _dbContext;

        public LogoutModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Revoke DB session token used for tracking metrics
            var sessionTokenClaim = User.FindFirst("SessionToken")?.Value;
            if (!string.IsNullOrEmpty(sessionTokenClaim))
            {
                var session = await _dbContext.UserSessions.FirstOrDefaultAsync(s => s.Token == sessionTokenClaim);
                if (session != null)
                {
                    _dbContext.UserSessions.Remove(session);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // Sign out of the cookie context
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessMessage"] = "Logged out successfully.";
            return RedirectToPage("/Index");
        }
    }
}

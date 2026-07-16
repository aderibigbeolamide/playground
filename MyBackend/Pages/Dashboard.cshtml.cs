using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MyBackend.Data;
using MyBackend.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // Statistics Metrics
        public int TotalUsers { get; set; }
        public int ActiveSessions { get; set; }
        public int NewSignupsToday { get; set; }
        public double ActivePercentage { get; set; }
        public double DbResponseTimeMs { get; set; }
        
        // Trend Data
        public List<DailySignupDto> SignupTrend { get; set; } = new();
        public List<SvgPoint> SvgPoints { get; set; } = new();
        public string LinePath { get; set; } = string.Empty;
        public string AreaPath { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            // Retrieve User Info
            Username = User.Identity?.Name ?? "Guest";
            Email = User.FindFirstValue(ClaimTypes.Email) ?? "user@example.com";

            var sw = Stopwatch.StartNew();

            // Total users count
            TotalUsers = await _dbContext.Users.CountAsync();

            // Active sessions count (not expired)
            ActiveSessions = await _dbContext.UserSessions.CountAsync(s => s.ExpiresAt > DateTime.UtcNow);

            // New signups today (UTC)
            var today = DateTime.UtcNow.Date;
            NewSignupsToday = await _dbContext.Users.CountAsync(u => u.CreatedAt >= today);

            // Mock database latency check (real time taken for queries so far)
            sw.Stop();
            DbResponseTimeMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);

            // Calculate active user percentage
            ActivePercentage = TotalUsers > 0 
                ? Math.Round(((double)ActiveSessions / TotalUsers) * 100, 1) 
                : 0;

            if (ActivePercentage > 100) ActivePercentage = 100;

            // Generate daily signup trend for the last 7 days
            SignupTrend.Clear();
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = DateTime.UtcNow.AddDays(-i).Date;
                var nextDate = targetDate.AddDays(1);
                var count = await _dbContext.Users.CountAsync(u => u.CreatedAt >= targetDate && u.CreatedAt < nextDate);
                
                SignupTrend.Add(new DailySignupDto(
                    targetDate.ToString("yyyy-MM-dd"),
                    count
                ));
            }

            // Calculate Server-Side SVG Chart Coordinates
            CalculateSvgCoordinates();

            return Page();
        }

        private void CalculateSvgCoordinates()
        {
            if (SignupTrend.Count == 0) return;

            const double width = 500;
            const double height = 220;
            const double paddingLeft = 40;
            const double paddingRight = 20;
            const double paddingTop = 30;
            const double paddingBottom = 40;

            const double chartWidth = width - paddingLeft - paddingRight;
            const double chartHeight = height - paddingTop - paddingBottom;

            var counts = SignupTrend.Select(d => d.Count).ToList();
            int maxVal = counts.Max();
            if (maxVal < 5) maxVal = 5; // default scale threshold
            int minVal = 0;
            int valRange = maxVal - minVal;

            int pointsCount = SignupTrend.Count;
            double xInterval = chartWidth / (pointsCount - 1);

            SvgPoints.Clear();
            var coords = new List<Point>();

            for (int i = 0; i < pointsCount; i++)
            {
                var item = SignupTrend[i];
                double x = paddingLeft + (i * xInterval);
                double ratio = valRange > 0 ? (double)(item.Count - minVal) / valRange : 0;
                double y = height - paddingBottom - (ratio * chartHeight);

                var shortDate = FormatShortDate(item.Date);
                var fullDate = FormatDateString(item.Date);

                SvgPoints.Add(new SvgPoint(x, y, shortDate, fullDate, item.Count));
                coords.Add(new Point(x, y));
            }

            // Construct LinePath and AreaPath strings
            var lineSegments = new List<string>();
            for (int i = 0; i < coords.Count; i++)
            {
                var prefix = i == 0 ? "M" : "L";
                lineSegments.Add($"{prefix} {coords[i].X:F1} {coords[i].Y:F1}");
            }
            LinePath = string.Join(" ", lineSegments);

            if (coords.Count > 0)
            {
                AreaPath = $"M {coords[0].X:F1} {(height - paddingBottom):F1} " + 
                           string.Join(" ", coords.Select(c => $"L {c.X:F1} {c.Y:F1}")) + 
                           $" L {coords[coords.Count - 1].X:F1} {(height - paddingBottom):F1} Z";
            }
        }

        private string FormatShortDate(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var date))
            {
                return date.ToString("MMM d");
            }
            return dateStr;
        }

        private string FormatDateString(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var date))
            {
                return date.ToString("MMMM d, yyyy");
            }
            return dateStr;
        }
    }

    public record DailySignupDto(string Date, int Count);
    public record SvgPoint(double X, double Y, string ShortDate, string FullDate, int Count);
    internal record Point(double X, double Y);
}

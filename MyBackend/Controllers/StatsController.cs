using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBackend.Data;
using MyBackend.DTOs;
using MyBackend.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MyBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public StatsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            // Simple authorization check
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized(new { message = "Unauthorized access to statistics." });
            }

            var sw = Stopwatch.StartNew();

            // Total users count
            var totalUsers = await _dbContext.Users.CountAsync();

            // Active sessions count (not expired)
            var activeSessions = await _dbContext.UserSessions.CountAsync(s => s.ExpiresAt > DateTime.UtcNow);

            // New signups today (UTC)
            var today = DateTime.UtcNow.Date;
            var newSignupsToday = await _dbContext.Users.CountAsync(u => u.CreatedAt >= today);

            // Mock database latency check (real time taken for queries so far)
            sw.Stop();
            var dbTime = sw.Elapsed.TotalMilliseconds;

            // Calculate active user percentage
            double activePercentage = totalUsers > 0 
                ? Math.Round(((double)activeSessions / totalUsers) * 100, 1) 
                : 0;

            if (activePercentage > 100) activePercentage = 100;

            // Generate daily signup trend for the last 7 days
            var signupTrend = new List<DailySignupDto>();
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = DateTime.UtcNow.AddDays(-i).Date;
                var nextDate = targetDate.AddDays(1);
                var count = await _dbContext.Users.CountAsync(u => u.CreatedAt >= targetDate && u.CreatedAt < nextDate);
                
                signupTrend.Add(new DailySignupDto(
                    targetDate.ToString("yyyy-MM-dd"),
                    count
                ));
            }

            var stats = new DashboardStatsDto(
                totalUsers,
                activeSessions,
                newSignupsToday,
                activePercentage,
                Math.Round(dbTime, 2),
                signupTrend
            );

            return Ok(stats);
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var session = await _dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

            if (session == null)
            {
                return null;
            }

            return await _dbContext.Users.FindAsync(session.UserId);
        }
    }
}

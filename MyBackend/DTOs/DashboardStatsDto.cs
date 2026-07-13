using System.Collections.Generic;

namespace MyBackend.DTOs
{
    public record DashboardStatsDto(
        int TotalUsers,
        int ActiveSessions,
        int NewSignupsToday,
        double ActivePercentage,
        double DbResponseTimeMs,
        List<DailySignupDto> SignupTrend
    );

    public record DailySignupDto(
        string Date,
        int Count
    );
}

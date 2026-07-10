using System;
using System.Collections.Generic;

namespace BLL.DTOs.Tokens;

public class UserTokenUsageDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public short RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public long ChatTokens { get; set; }
    public long DocTokens { get; set; }
    public long TotalTokens => ChatTokens + DocTokens;
    public double PercentOfQuota { get; set; }
    public List<int> SparklineData { get; set; } = new();
    public List<int> ChatHistoryData { get; set; } = new();
    public List<int> DocHistoryData { get; set; } = new();
    public List<string> DateLabels { get; set; } = new();

    public string Initial => string.IsNullOrWhiteSpace(FullName) ? "U" : FullName.Trim().Substring(0, 1).ToUpper();
}

public class HeroStatsDto
{
    public long TotalUsedTokens { get; set; }
    public long TotalQuotaTokens { get; set; }
    public double OverallPercent => TotalQuotaTokens > 0 ? Math.Round((double)TotalUsedTokens / TotalQuotaTokens * 100, 1) : 0;
    public long TotalChatTokens { get; set; }
    public long TotalDocTokens { get; set; }
    public UserTokenUsageDto? TopConsumer { get; set; }
    public int DailyAvgTokens { get; set; }
    public int WeeklyApiRequests { get; set; }
}

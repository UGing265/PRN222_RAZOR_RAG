using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Tokens;
using BLL.Interfaces.Tokens;
using DAL.Interfaces.Auth;
using DAL.Interfaces.Tokens;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Tokens;

public class TokenUsageService : ITokenUsageService
{
    private readonly ITokenUsageRepository _tokenRepo;
    private readonly IAuthRepository _authRepo;
    private readonly ILogger<TokenUsageService> _logger;

    public TokenUsageService(
        ITokenUsageRepository tokenRepo,
        IAuthRepository authRepo,
        ILogger<TokenUsageService> logger)
    {
        _tokenRepo = tokenRepo;
        _authRepo = authRepo;
        _logger = logger;
    }

    public async Task RecordChatTokensAsync(Guid userId, int tokens, CancellationToken cancellationToken = default)
    {
        if (tokens <= 0) return;
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            await _tokenRepo.IncrementChatTokensAsync(userId, today, tokens, cancellationToken);
            _logger.LogInformation("Recorded {Tokens} chat tokens for user {UserId}", tokens, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record chat tokens for user {UserId}", userId);
        }
    }

    public async Task RecordDocTokensAsync(Guid userId, int tokens, CancellationToken cancellationToken = default)
    {
        if (tokens <= 0) return;
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            await _tokenRepo.IncrementDocTokensAsync(userId, today, tokens, cancellationToken);
            _logger.LogInformation("Recorded {Tokens} doc (embedding) tokens for user {UserId}", tokens, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record doc tokens for user {UserId}", userId);
        }
    }

    public async Task<(List<UserTokenUsageDto> users, HeroStatsDto heroStats)> GetTokenUsageReportAsync(long quotaTokens = 200000, CancellationToken cancellationToken = default)
    {
        var allUsers = await _authRepo.GetAllUsersWithRolesAsync(cancellationToken);
        var allUsages = await _tokenRepo.GetAllWithUserAsync(cancellationToken);

        // Lấy danh sách 7 ngày liên tiếp gần nhất (kết thúc bởi hôm nay)
        var dateLabels = new List<string>();
        var dates = new List<DateOnly>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int i = 6; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            dates.Add(d);
            dateLabels.Add(d.ToString("dd/MM"));
        }

        var userList = new List<UserTokenUsageDto>();

        foreach (var user in allUsers)
        {
            var userUsages = allUsages.Where(t => t.UserId == user.Id).ToList();

            long totalChat = userUsages.Sum(t => (long)t.ChatTokens);
            // Theo nghiệp vụ: Sinh viên (RoleId == 3) chỉ chat, lượng DocTokens = 0. Giảng viên (RoleId == 2) mới có DocTokens.
            long totalDoc = user.RoleId == 2 ? userUsages.Sum(t => (long)t.DocTokens) : 0;

            var sparkline = new List<int>();
            var chatHistory = new List<int>();
            var docHistory = new List<int>();

            foreach (var d in dates)
            {
                var dayRecord = userUsages.FirstOrDefault(t => t.UsageDate == d);
                int chatDay = dayRecord != null ? dayRecord.ChatTokens : 0;
                int docDay = (dayRecord != null && user.RoleId == 2) ? dayRecord.DocTokens : 0;

                chatHistory.Add(chatDay);
                docHistory.Add(docDay);
                sparkline.Add(chatDay + docDay);
            }

            var dto = new UserTokenUsageDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name switch
                {
                    "Admin" => "Admin",
                    "Lecturer" => "Giảng Viên",
                    "Student" => "Sinh Viên",
                    _ => user.Role?.Name ?? "Người Dùng"
                },
                ChatTokens = totalChat,
                DocTokens = totalDoc,
                PercentOfQuota = quotaTokens > 0 ? Math.Round((double)(totalChat + totalDoc) / quotaTokens * 100, 1) : 0,
                SparklineData = sparkline,
                ChatHistoryData = chatHistory,
                DocHistoryData = docHistory,
                DateLabels = dateLabels
            };

            userList.Add(dto);
        }

        var topConsumer = userList.OrderByDescending(u => u.TotalTokens).FirstOrDefault();
        var sumAllTotal = userList.Sum(u => u.TotalTokens);
        var sumChat = userList.Sum(u => u.ChatTokens);
        var sumDoc = userList.Sum(u => u.DocTokens);

        // Tổng token 7 ngày qua để tính trung bình ngày và lượt gọi API
        var last7DaysTotal = userList.Sum(u => u.SparklineData.Sum());
        var dailyAvg = (int)Math.Round(last7DaysTotal / 7.0);
        int weeklyApiRequests = Math.Max(1, (int)(last7DaysTotal / 15));

        var heroStats = new HeroStatsDto
        {
            TotalUsedTokens = sumAllTotal,
            TotalQuotaTokens = quotaTokens,
            TotalChatTokens = sumChat,
            TotalDocTokens = sumDoc,
            TopConsumer = topConsumer,
            DailyAvgTokens = dailyAvg,
            WeeklyApiRequests = weeklyApiRequests
        };

        return (userList, heroStats);
    }
}

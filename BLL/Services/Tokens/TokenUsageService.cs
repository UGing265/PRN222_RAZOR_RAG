using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Tokens;
using BLL.Interfaces;
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
    private readonly BLL.Interfaces.Notifications.INotificationService _notificationService;
    private readonly ISystemSettingService _systemSettingService;

    public TokenUsageService(
        ITokenUsageRepository tokenRepo,
        IAuthRepository authRepo,
        ILogger<TokenUsageService> logger,
        BLL.Interfaces.Notifications.INotificationService notificationService,
        ISystemSettingService systemSettingService)
    {
        _tokenRepo = tokenRepo;
        _authRepo = authRepo;
        _logger = logger;
        _notificationService = notificationService;
        _systemSettingService = systemSettingService;
    }

    public async Task RecordChatTokensAsync(Guid userId, int tokens, CancellationToken cancellationToken = default)
    {
        if (tokens <= 0) return;
        try
        {
            var now = DateTime.UtcNow.AddHours(7); // Convert to Vietnam Time (GMT+7)
            var today = DateOnly.FromDateTime(now);
            var hour = (byte)now.Hour;
            await _tokenRepo.IncrementChatTokensAsync(userId, today, hour, tokens, cancellationToken);
            _logger.LogInformation("Recorded {Tokens} chat tokens for user {UserId} at hour {Hour}", tokens, userId, hour);
            await _notificationService.SendTokenUsageUpdatedAsync(cancellationToken);
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
            var now = DateTime.UtcNow.AddHours(7); // Convert to Vietnam Time (GMT+7)
            var today = DateOnly.FromDateTime(now);
            var hour = (byte)now.Hour;
            await _tokenRepo.IncrementDocTokensAsync(userId, today, hour, tokens, cancellationToken);
            _logger.LogInformation("Recorded {Tokens} doc (embedding) tokens for user {UserId} at hour {Hour}", tokens, userId, hour);
            await _notificationService.SendTokenUsageUpdatedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record doc tokens for user {UserId}", userId);
        }
    }

    public async Task<(List<UserTokenUsageDto> users, HeroStatsDto heroStats)> GetTokenUsageReportAsync(CancellationToken cancellationToken = default)
    {
        var allUsers = await _authRepo.GetAllUsersWithRolesAsync(cancellationToken);
        var allUsages = await _tokenRepo.GetAllWithUserAsync(cancellationToken);

        // Lấy danh sách 7 ngày liên tiếp gần nhất (kết thúc bởi hôm nay theo giờ Việt Nam)
        var dateLabels = new List<string>();
        var dates = new List<DateOnly>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

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

            // Calculate hourly data for "today"
            var chatHourly = new List<int>(new int[24]);
            var docHourly = new List<int>(new int[24]);

            var todayUsages = userUsages.Where(t => t.UsageDate == today).ToList();
            foreach (var record in todayUsages)
            {
                if (record.UsageHour >= 0 && record.UsageHour < 24)
                {
                    chatHourly[record.UsageHour] += record.ChatTokens;
                    if (user.RoleId == 2) docHourly[record.UsageHour] += record.DocTokens;
                }
            }

            foreach (var d in dates)
            {
                var dayRecords = userUsages.Where(t => t.UsageDate == d).ToList();
                int chatDay = dayRecords.Sum(t => t.ChatTokens);
                int docDay = user.RoleId == 2 ? dayRecords.Sum(t => t.DocTokens) : 0;

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

                SparklineData = sparkline,
                ChatHistoryData = chatHistory,
                DocHistoryData = docHistory,
                DateLabels = dateLabels,
                ChatHourlyData = chatHourly,
                DocHourlyData = docHourly
            };

            userList.Add(dto);
        }

        var topConsumer = userList.OrderByDescending(u => u.TotalTokens).FirstOrDefault();
        var sumAllTotal = userList.Sum(u => u.TotalTokens);
        var sumChat = userList.Sum(u => u.ChatTokens);
        var sumDoc = userList.Sum(u => u.DocTokens);

        // Tổng token 7 ngày qua để tính trung bình ngày
        var last7DaysTotal = userList.Sum(u => u.SparklineData.Sum());
        var dailyAvg = (int)Math.Round(last7DaysTotal / 7.0);

        // Tính tổng dữ liệu theo từng ngày cho toàn hệ thống
        var systemChatHistory = new List<int>();
        var systemDocHistory = new List<int>();
        for (int i = 0; i < dates.Count; i++)
        {
            systemChatHistory.Add(userList.Sum(u => u.ChatHistoryData[i]));
            systemDocHistory.Add(userList.Sum(u => u.DocHistoryData[i]));
        }

        // Tính tổng dữ liệu theo từng giờ cho toàn hệ thống
        var systemChatHourly = new List<int>();
        var systemDocHourly = new List<int>();
        for (int i = 0; i < 24; i++)
        {
            systemChatHourly.Add(userList.Sum(u => u.ChatHourlyData[i]));
            systemDocHourly.Add(userList.Sum(u => u.DocHourlyData[i]));
        }

        var heroStats = new HeroStatsDto
        {
            TotalUsedTokens = sumAllTotal,
            TotalChatTokens = sumChat,
            TotalDocTokens = sumDoc,
            TopConsumer = topConsumer,
            DailyAvgTokens = dailyAvg,
            DateLabels = dateLabels,
            SystemChatHistoryData = systemChatHistory,
            SystemDocHistoryData = systemDocHistory,
            SystemChatHourlyData = systemChatHourly,
            SystemDocHourlyData = systemDocHourly
        };

        return (userList, heroStats);
    }

    public async Task<bool> IsDailyLimitExceededAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _authRepo.GetUserByIdAsync(userId, cancellationToken);
        if (user == null) return false;

        int limit = 0;
        if (user.RoleId == 3) // Student
        {
            limit = await _systemSettingService.GetStudentDailyTokenLimitAsync(cancellationToken);
        }
        else if (user.RoleId == 2) // Lecturer
        {
            limit = await _systemSettingService.GetLecturerDailyTokenLimitAsync(cancellationToken);
        }
        else
        {
            return false; // Admin or other roles have no limit
        }

        if (limit <= 0) return false;

        var now = DateTime.UtcNow.AddHours(7);
        var today = DateOnly.FromDateTime(now);
        
        var todayUsages = await _tokenRepo.GetByUserAndDateRangeAsync(userId, today, today, cancellationToken);
        long totalToday = todayUsages.Sum(u => (long)u.ChatTokens + u.DocTokens);

        return totalToday >= limit;
    }
}

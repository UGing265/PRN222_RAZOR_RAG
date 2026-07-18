using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Tokens;

namespace BLL.Interfaces.Tokens;

public interface ITokenUsageService
{
    Task RecordChatTokensAsync(Guid userId, int tokens, CancellationToken cancellationToken = default);
    Task RecordDocTokensAsync(Guid userId, int tokens, CancellationToken cancellationToken = default);
    Task<(List<UserTokenUsageDto> users, HeroStatsDto heroStats)> GetTokenUsageReportAsync(CancellationToken cancellationToken = default);
    Task<bool> IsDailyLimitExceededAsync(Guid userId, CancellationToken cancellationToken = default);
}

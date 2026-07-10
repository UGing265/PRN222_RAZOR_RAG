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
    Task<(List<UserTokenUsageDto> users, HeroStatsDto heroStats)> GetTokenUsageReportAsync(long quotaTokens = 200000, CancellationToken cancellationToken = default);
}

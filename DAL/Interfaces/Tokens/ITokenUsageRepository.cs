using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DAL.Entities;

namespace DAL.Interfaces.Tokens;

public interface ITokenUsageRepository
{
    Task<TokenUsage?> GetByUserIdAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);
    Task IncrementChatTokensAsync(Guid userId, DateOnly date, int tokensToAdd, CancellationToken cancellationToken = default);
    Task IncrementDocTokensAsync(Guid userId, DateOnly date, int tokensToAdd, CancellationToken cancellationToken = default);
    Task<List<TokenUsage>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<List<TokenUsage>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<List<TokenUsage>> GetAllWithUserAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

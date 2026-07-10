using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAL.Data;
using DAL.Entities;
using DAL.Interfaces.Tokens;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Tokens;

public class TokenUsageRepository : ITokenUsageRepository
{
    private readonly DBContext _context;

    public TokenUsageRepository(DBContext context)
    {
        _context = context;
    }

    public async Task<TokenUsage?> GetByUserIdAndDateAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.TokenUsages
            .FirstOrDefaultAsync(t => t.UserId == userId && t.UsageDate == date, cancellationToken);
    }

    public async Task IncrementChatTokensAsync(Guid userId, DateOnly date, int tokensToAdd, CancellationToken cancellationToken = default)
    {
        var existing = await _context.TokenUsages
            .FirstOrDefaultAsync(t => t.UserId == userId && t.UsageDate == date, cancellationToken);

        if (existing != null)
        {
            existing.ChatTokens += tokensToAdd;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var usage = new TokenUsage
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UsageDate = date,
                ChatTokens = tokensToAdd,
                DocTokens = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TokenUsages.Add(usage);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementDocTokensAsync(Guid userId, DateOnly date, int tokensToAdd, CancellationToken cancellationToken = default)
    {
        var existing = await _context.TokenUsages
            .FirstOrDefaultAsync(t => t.UserId == userId && t.UsageDate == date, cancellationToken);

        if (existing != null)
        {
            existing.DocTokens += tokensToAdd;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var usage = new TokenUsage
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UsageDate = date,
                ChatTokens = 0,
                DocTokens = tokensToAdd,
                UpdatedAt = DateTime.UtcNow
            };
            _context.TokenUsages.Add(usage);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<TokenUsage>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _context.TokenUsages
            .Where(t => t.UsageDate >= startDate && t.UsageDate <= endDate)
            .OrderBy(t => t.UsageDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TokenUsage>> GetByUserAndDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _context.TokenUsages
            .Where(t => t.UserId == userId && t.UsageDate >= startDate && t.UsageDate <= endDate)
            .OrderBy(t => t.UsageDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TokenUsage>> GetAllWithUserAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TokenUsages
            .Include(t => t.User)
            .ThenInclude(u => u.Role)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

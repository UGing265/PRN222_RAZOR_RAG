using DAL.Data;
using DAL.Entities;
using DAL.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Auth;

public class AuthRepository : IAuthRepository
{
    private readonly DBContext _dbContext;

    public AuthRepository(DBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<bool> RoleExistsAsync(short roleId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Roles.AnyAsync(x => x.Id == roleId, cancellationToken);
    }

    public async Task<User> AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public Task<User?> GetUserByEmailWithRoleAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<List<User>> GetAllUsersWithRolesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(x => x.Role)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAuditLogAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<AuditLog> Items, int TotalCount, Dictionary<Guid, string> TargetNames)> GetAuditLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().Include(x => x.User);
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        // Resolve TargetNames
        var targetNames = new Dictionary<Guid, string>();
        
        var userIds = items.Where(x => x.TargetTable == "Users").Select(x => x.TargetId).Distinct().ToList();
        if (userIds.Any())
        {
            var users = await _dbContext.Users.AsNoTracking().Where(x => userIds.Contains(x.Id)).Select(x => new { x.Id, x.FullName }).ToListAsync(cancellationToken);
            foreach (var user in users) targetNames[user.Id] = user.FullName;
        }

        var documentIds = items.Where(x => x.TargetTable == "Documents").Select(x => x.TargetId).Distinct().ToList();
        if (documentIds.Any())
        {
            var docs = await _dbContext.Documents.AsNoTracking().Where(x => documentIds.Contains(x.Id)).Select(x => new { x.Id, x.Title }).ToListAsync(cancellationToken);
            foreach (var doc in docs) targetNames[doc.Id] = doc.Title;
        }

        return (items, totalCount, targetNames);
    }
}

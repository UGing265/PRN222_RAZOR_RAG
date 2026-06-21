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

    public Task<bool> AccountRequestEmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return _dbContext.AccountRequests.AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<AccountRequest> AddAccountRequestAsync(AccountRequest request, CancellationToken cancellationToken = default)
    {
        _dbContext.AccountRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return request;
    }

    public Task<List<AccountRequest>> GetPendingAccountRequestsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.AccountRequests
            .Include(x => x.Role)
            .Where(x => x.Status == "pending" && x.VerificationToken == null)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<AccountRequest?> GetAccountRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return _dbContext.AccountRequests
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);
    }

    public Task<AccountRequest?> GetAccountRequestByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return _dbContext.AccountRequests
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.VerificationToken == token && x.Status == "pending", cancellationToken);
    }

    public async Task UpdateAccountRequestAsync(AccountRequest request, CancellationToken cancellationToken = default)
    {
        _dbContext.AccountRequests.Update(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
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
}

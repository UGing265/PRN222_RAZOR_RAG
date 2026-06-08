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

    public Task<Session?> GetSessionWithUserAndRoleAsync(string token, CancellationToken cancellationToken = default)
    {
        return _dbContext.Sessions
            .Include(s => s.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(s => s.Token == token, cancellationToken);
    }

    public async Task DeleteSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Token == token, cancellationToken);
        if (session != null)
        {
            _dbContext.Sessions.Remove(session);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

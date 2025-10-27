using Microsoft.EntityFrameworkCore;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Entities;

namespace InvestmentHub.Infrastructure.Data.Repositories;

/// <summary>
/// Entity Framework Core implementation of the IUserRepository.
/// Provides data access methods for User entity operations.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Id == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetPortfolioCountAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Portfolios
            .CountAsync(p => p.OwnerId == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CanCreatePortfolioAsync(UserId userId, int maxPortfolios = 10, CancellationToken cancellationToken = default)
    {
        var portfolioCount = await GetPortfolioCountAsync(userId, cancellationToken);
        return portfolioCount < maxPortfolios;
    }
}


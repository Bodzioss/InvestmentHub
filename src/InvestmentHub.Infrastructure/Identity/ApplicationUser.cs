using Microsoft.AspNetCore.Identity;

namespace InvestmentHub.Infrastructure.Identity;

/// <summary>
/// Identity user for authentication.
/// Separated from Domain.User to maintain clean architecture.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
}

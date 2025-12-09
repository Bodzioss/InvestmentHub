using System.Security.Claims;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace InvestmentHub.API.Authorization;

public class PortfolioOwnerRequirement : IAuthorizationRequirement
{
}

public class PortfolioOwnerHandler : AuthorizationHandler<PortfolioOwnerRequirement, Guid>
{
    private readonly IPortfolioRepository _portfolioRepository;
    public PortfolioOwnerHandler(IPortfolioRepository portfolioRepository)
    {
        _portfolioRepository = portfolioRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        PortfolioOwnerRequirement requirement, 
        Guid portfolioId)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return;
        }

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return;
        }

        var portfolio = await _portfolioRepository.GetByIdAsync(new PortfolioId(portfolioId));
        if (portfolio == null)
        {
            // If resource doesn't exist, we generally let it pass authorization 
            // so the controller can return 404 Not Found instead of 403 Forbidden
            context.Succeed(requirement);
            return;
        }

        if (portfolio.OwnerId.Value == userId)
        {
            context.Succeed(requirement);
        }
    }
}

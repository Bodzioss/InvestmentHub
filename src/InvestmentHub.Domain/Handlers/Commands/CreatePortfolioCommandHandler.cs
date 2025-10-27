using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for CreatePortfolioCommand.
/// Responsible for creating a new portfolio with full business logic validation.
/// </summary>
public class CreatePortfolioCommandHandler : IRequestHandler<CreatePortfolioCommand, CreatePortfolioResult>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the CreatePortfolioCommandHandler class.
    /// </summary>
    /// <param name="portfolioRepository">The portfolio repository</param>
    /// <param name="userRepository">The user repository</param>
    public CreatePortfolioCommandHandler(
        IPortfolioRepository portfolioRepository,
        IUserRepository userRepository)
    {
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Handles the CreatePortfolioCommand.
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<CreatePortfolioResult> Handle(CreatePortfolioCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();
            
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return CreatePortfolioResult.Failure("Portfolio name cannot be empty");
            }

            // 1. Validate user exists
            var user = await _userRepository.GetByIdAsync(request.OwnerId, cancellationToken);
            if (user == null)
            {
                return CreatePortfolioResult.Failure("User not found");
            }

            // 2. Check if user can create more portfolios
            var canCreate = await _userRepository.CanCreatePortfolioAsync(request.OwnerId, 10, cancellationToken);
            if (!canCreate)
            {
                return CreatePortfolioResult.Failure("User has reached the maximum number of portfolios");
            }

            // 3. Check for duplicate portfolio name
            var existsByName = await _portfolioRepository.ExistsByNameAsync(request.OwnerId, request.Name, cancellationToken);
            if (existsByName)
            {
                return CreatePortfolioResult.Failure($"Portfolio with name '{request.Name}' already exists for this user");
            }

            // 4. Create new portfolio
            var portfolio = new Portfolio(
                request.PortfolioId,
                request.Name,
                request.Description,
                request.OwnerId);

            // 5. Save portfolio
            await _portfolioRepository.AddAsync(portfolio, cancellationToken);

            // 6. Return success
            return CreatePortfolioResult.Success(request.PortfolioId);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            return CreatePortfolioResult.Failure($"Failed to create portfolio: {ex.Message}");
        }
    }
}

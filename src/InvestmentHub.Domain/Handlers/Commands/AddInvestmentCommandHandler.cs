using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for AddInvestmentCommand.
/// Responsible for adding a new investment to a portfolio with full business logic validation.
/// </summary>
public class AddInvestmentCommandHandler : IRequestHandler<AddInvestmentCommand, AddInvestmentResult>
{
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Initializes a new instance of the AddInvestmentCommandHandler class.
    /// </summary>
    /// <param name="portfolioRepository">The portfolio repository</param>
    /// <param name="investmentRepository">The investment repository</param>
    /// <param name="userRepository">The user repository</param>
    public AddInvestmentCommandHandler(
        IPortfolioRepository portfolioRepository,
        IInvestmentRepository investmentRepository,
        IUserRepository userRepository)
    {
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
        _investmentRepository = investmentRepository ?? throw new ArgumentNullException(nameof(investmentRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Handles the AddInvestmentCommand.
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<AddInvestmentResult> Handle(AddInvestmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancellation was requested");
            }
            
            // Validate the request
            if (request.Quantity <= 0)
            {
                return AddInvestmentResult.Failure("Quantity must be positive");
            }

            if (request.PurchaseDate > DateTime.UtcNow)
            {
                return AddInvestmentResult.Failure("Purchase date cannot be in the future");
            }

            // 1. Load and validate portfolio exists
            var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId, cancellationToken);
            if (portfolio == null)
            {
                return AddInvestmentResult.Failure("Portfolio not found");
            }

            // 2. Validate user exists and has access to portfolio
            var user = await _userRepository.GetByIdAsync(portfolio.OwnerId, cancellationToken);
            if (user == null)
            {
                return AddInvestmentResult.Failure("User not found");
            }

            // 3. Check for duplicate symbol in portfolio
            var existingInvestment = await _investmentRepository.ExistsBySymbolAsync(request.PortfolioId, request.Symbol, cancellationToken);
            if (existingInvestment)
            {
                return AddInvestmentResult.Failure($"Investment with symbol {request.Symbol.Ticker} already exists in this portfolio");
            }

            // 4. Check portfolio investment limits
            var investmentCount = await _investmentRepository.GetCountByPortfolioIdAsync(request.PortfolioId, cancellationToken);
            const int maxInvestmentsPerPortfolio = 100;
            if (investmentCount >= maxInvestmentsPerPortfolio)
            {
                return AddInvestmentResult.Failure($"Portfolio cannot have more than {maxInvestmentsPerPortfolio} investments");
            }

            // 5. Create the investment
            var investmentId = InvestmentId.New();
            var investment = new Investment(
                investmentId,
                request.PortfolioId,
                request.Symbol,
                request.PurchasePrice,
                request.Quantity,
                request.PurchaseDate);

            // 6. Add investment to portfolio (this will trigger domain events)
            portfolio.AddInvestment(investment);

            // 7. Save changes
            await _investmentRepository.AddAsync(investment, cancellationToken);
            await _portfolioRepository.UpdateAsync(portfolio, cancellationToken);

            // 8. Return success
            return AddInvestmentResult.Success(investmentId);
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation exceptions
            throw;
        }
        catch (Exception ex)
        {
            return AddInvestmentResult.Failure($"Failed to add investment: {ex.Message}");
        }
    }
}

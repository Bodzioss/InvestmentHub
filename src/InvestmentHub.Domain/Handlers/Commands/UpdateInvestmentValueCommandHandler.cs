using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for UpdateInvestmentValueCommand.
/// Responsible for updating an investment's current market value.
/// </summary>
public class UpdateInvestmentValueCommandHandler : IRequestHandler<UpdateInvestmentValueCommand, UpdateInvestmentValueResult>
{
    private readonly IInvestmentRepository _investmentRepository;
    private readonly ILogger<UpdateInvestmentValueCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the UpdateInvestmentValueCommandHandler class.
    /// </summary>
    /// <param name="investmentRepository">The investment repository</param>
    /// <param name="logger">The logger</param>
    public UpdateInvestmentValueCommandHandler(
        IInvestmentRepository investmentRepository,
        ILogger<UpdateInvestmentValueCommandHandler> logger)
    {
        _investmentRepository = investmentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the UpdateInvestmentValueCommand.
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The result of the operation</returns>
    public async Task<UpdateInvestmentValueResult> Handle(UpdateInvestmentValueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating investment value for {InvestmentId}", request.InvestmentId.Value);

            // 1. Load the investment from repository
            var investment = await _investmentRepository.GetByIdAsync(request.InvestmentId, cancellationToken);
            
            if (investment == null)
            {
                _logger.LogWarning("Investment {InvestmentId} not found", request.InvestmentId.Value);
                return UpdateInvestmentValueResult.Failure("Investment not found");
            }

            // 2. Update the current value
            investment.UpdateCurrentValue(request.CurrentPrice);

            // 3. Save changes
            await _investmentRepository.UpdateAsync(investment, cancellationToken);

            _logger.LogInformation("Successfully updated investment {InvestmentId} to {CurrentValue}", 
                request.InvestmentId.Value, investment.CurrentValue);

            // 4. Return success with the new total value
            return UpdateInvestmentValueResult.Success(investment.CurrentValue);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update investment {InvestmentId}: {Message}", 
                request.InvestmentId.Value, ex.Message);
            return UpdateInvestmentValueResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update investment value for {InvestmentId}", 
                request.InvestmentId.Value);
            return UpdateInvestmentValueResult.Failure($"Failed to update investment value: {ex.Message}");
        }
    }
}

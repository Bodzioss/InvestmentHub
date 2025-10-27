using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ValueObjects;
using MediatR;

namespace InvestmentHub.Domain.Handlers.Commands;

/// <summary>
/// Handler for UpdateInvestmentValueCommand.
/// Responsible for updating an investment's current market value.
/// </summary>
public class UpdateInvestmentValueCommandHandler : IRequestHandler<UpdateInvestmentValueCommand, UpdateInvestmentValueResult>
{
    // In a real application, this would inject repositories and services

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
            // In a real application, you would:
            // 1. Load the investment from repository
            // 2. Validate investment exists and is active
            // 3. Update the current value
            // 4. Save changes
            // 5. Publish domain events

            // Simulate async operation
            await Task.Delay(10, cancellationToken);

            // Simulate finding the investment and updating its value
            // In real app, this would be: investment.UpdateCurrentValue(request.CurrentPrice);
            var updatedValue = request.CurrentPrice.Multiply(10m); // Simulate quantity of 10

            return UpdateInvestmentValueResult.Success(updatedValue);
        }
        catch (Exception ex)
        {
            return UpdateInvestmentValueResult.Failure($"Failed to update investment value: {ex.Message}");
        }
    }
}

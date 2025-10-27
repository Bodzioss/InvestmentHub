using InvestmentHub.Domain.ValueObjects;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace InvestmentHub.Domain.Commands;

/// <summary>
/// Command to delete an investment.
/// This represents a write operation that removes an investment from the system.
/// </summary>
public record DeleteInvestmentCommand : IRequest<DeleteInvestmentResult>
{
    /// <summary>
    /// Gets the investment ID to delete.
    /// </summary>
    [Required(ErrorMessage = "Investment ID is required")]
    public InvestmentId InvestmentId { get; init; }

    /// <summary>
    /// Gets the reason for deletion.
    /// </summary>
    [StringLength(500, ErrorMessage = "Deletion reason cannot exceed 500 characters")]
    public string? Reason { get; init; }

    /// <summary>
    /// Gets a value indicating whether to force deletion even if investment has value.
    /// </summary>
    public bool ForceDelete { get; init; }

    /// <summary>
    /// Initializes a new instance of the DeleteInvestmentCommand class.
    /// </summary>
    /// <param name="investmentId">The investment ID</param>
    /// <param name="reason">The deletion reason (optional)</param>
    /// <param name="forceDelete">Whether to force deletion (optional)</param>
    public DeleteInvestmentCommand(
        InvestmentId investmentId,
        string? reason = null,
        bool forceDelete = false)
    {
        InvestmentId = investmentId;
        Reason = reason;
        ForceDelete = forceDelete;
    }
}

/// <summary>
/// Result of the DeleteInvestmentCommand operation.
/// </summary>
public record DeleteInvestmentResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the deleted investment ID if successful.
    /// </summary>
    public InvestmentId? InvestmentId { get; init; }

    /// <summary>
    /// Gets the deletion timestamp if successful.
    /// </summary>
    public DateTime? DeletedAt { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="investmentId">The deleted investment ID</param>
    /// <returns>A successful result</returns>
    public static DeleteInvestmentResult Success(InvestmentId investmentId) =>
        new() { IsSuccess = true, InvestmentId = investmentId, DeletedAt = DateTime.UtcNow };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <returns>A failed result</returns>
    public static DeleteInvestmentResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}

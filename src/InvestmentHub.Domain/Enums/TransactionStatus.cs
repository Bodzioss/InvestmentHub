namespace InvestmentHub.Domain.Enums;

/// <summary>
/// Status of a transaction
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is active and counted in calculations
    /// </summary>
    Active,

    /// <summary>
    /// Transaction has been cancelled and should be ignored
    /// </summary>
    Cancelled
}

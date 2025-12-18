namespace InvestmentHub.Domain.Enums;

/// <summary>
/// Types of transactions in a portfolio
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Purchase of stocks or bonds
    /// </summary>
    BUY,

    /// <summary>
    /// Sale of stocks or bonds
    /// </summary>
    SELL,

    /// <summary>
    /// Dividend payment from stocks
    /// </summary>
    DIVIDEND,

    /// <summary>
    /// Interest payment from bonds
    /// </summary>
    INTEREST
}

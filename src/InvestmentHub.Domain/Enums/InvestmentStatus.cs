namespace InvestmentHub.Domain.Enums;

/// <summary>
/// Represents the current status of an investment.
/// Used to track the lifecycle and availability of investments.
/// </summary>
public enum InvestmentStatus
{
    /// <summary>Investment is active and can be traded</summary>
    Active,
    
    /// <summary>Investment has been sold</summary>
    Sold,
    
    /// <summary>Investment is temporarily suspended from trading</summary>
    Suspended,
    
    /// <summary>Investment has been delisted from the exchange</summary>
    Delisted
}

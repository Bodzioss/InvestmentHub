namespace InvestmentHub.Domain.Enums;

/// <summary>
/// Represents the current status of a portfolio.
/// Used to manage portfolio lifecycle and access permissions.
/// </summary>
public enum PortfolioStatus
{
    /// <summary>Portfolio is active and can be modified</summary>
    Active,
    
    /// <summary>Portfolio is archived and read-only</summary>
    Archived,
    
    /// <summary>Portfolio is temporarily suspended</summary>
    Suspended
}

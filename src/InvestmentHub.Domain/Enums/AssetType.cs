namespace InvestmentHub.Domain.Enums;

/// <summary>
/// Represents different types of financial assets that can be traded.
/// Used to categorize investments and apply appropriate business rules.
/// </summary>
public enum AssetType
{
    /// <summary>Individual company stocks</summary>
    Stock,
    
    /// <summary>Government or corporate bonds</summary>
    Bond,
    
    /// <summary>Exchange-Traded Funds</summary>
    ETF,
    
    /// <summary>Mutual funds</summary>
    MutualFund,
    
    /// <summary>Cryptocurrencies</summary>
    Crypto,
    
    /// <summary>Commodities (gold, oil, etc.)</summary>
    Commodity,
    
    /// <summary>Foreign exchange currencies</summary>
    Forex,
    
    /// <summary>Options contracts</summary>
    Option,
    
    /// <summary>Futures contracts</summary>
    Future
}

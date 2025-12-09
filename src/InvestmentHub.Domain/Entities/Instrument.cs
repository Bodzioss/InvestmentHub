using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Domain.Entities;

/// <summary>
/// Represents a financial instrument available for trading.
/// This is reference data imported from external sources.
/// </summary>
public class Instrument
{
    /// <summary>
    /// Gets the unique identifier of the instrument.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the financial symbol details (Ticker, Exchange, AssetType).
    /// </summary>
    public Symbol Symbol { get; private set; }

    /// <summary>
    /// Gets the full name of the instrument.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the International Securities Identification Number.
    /// </summary>
    public string Isin { get; private set; }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    private Instrument() 
    {
        Symbol = null!;
        Name = null!;
        Isin = null!;
    }

    /// <summary>
    /// Initializes a new instance of the Instrument class.
    /// </summary>
    /// <param name="symbol">The symbol details</param>
    /// <param name="name">The full name</param>
    /// <param name="isin">The ISIN code</param>
    public Instrument(Symbol symbol, string name, string isin)
    {
        Id = Guid.NewGuid();
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Isin = isin ?? throw new ArgumentNullException(nameof(isin));
    }
}

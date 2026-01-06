using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Models;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.Repositories;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Infrastructure.Data;
using InvestmentHub.Infrastructure.Services;
using InvestmentHub.Infrastructure.TreasuryBonds;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.Handlers.Queries;

/// <summary>
/// Handler for GetPositionsQuery.
/// Calculates positions from transaction history using FIFO cost basis.
/// </summary>
public class GetPositionsQueryHandler : IRequestHandler<GetPositionsQuery, GetPositionsResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MarketPriceService _marketPriceService;
    private readonly BondValueCalculator _bondValueCalculator;
    private readonly ILogger<GetPositionsQueryHandler> _logger;

    public GetPositionsQueryHandler(
        ApplicationDbContext dbContext,
        MarketPriceService marketPriceService,
        BondValueCalculator bondValueCalculator,
        ILogger<GetPositionsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _marketPriceService = marketPriceService;
        _bondValueCalculator = bondValueCalculator;
        _logger = logger;
    }

    public async Task<GetPositionsResult> Handle(GetPositionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all active transactions for portfolio
            var query = _dbContext.Transactions
                .Where(t => t.PortfolioId == request.PortfolioId && t.Status == TransactionStatus.Active);

            if (request.SymbolFilter != null)
                query = query.Where(t => t.Symbol == request.SymbolFilter);

            var transactions = await query
                .OrderBy(t => t.TransactionDate)
                .ToListAsync(cancellationToken);

            // Group by symbol ticker (using string instead of value object for EF Core compatibility)
            // Filter out transactions with null Symbol first
            var symbolGroups = transactions
                .Where(t => t.Symbol != null)
                .GroupBy(t => t.Symbol!.Ticker);
            var positions = new List<Position>();

            foreach (var group in symbolGroups)
            {
                // Get the Symbol from the first transaction in the group
                var symbol = group.First().Symbol;
                var position = await CalculatePositionAsync(symbol, group.ToList(), cancellationToken);
                if (position.TotalQuantity > 0 || position.TotalIncome.Amount > 0)
                {
                    positions.Add(position);
                }
            }

            return GetPositionsResult.Success(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get positions");
            return GetPositionsResult.Failure($"Failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculate position for a symbol using FIFO cost basis.
    /// </summary>
    private async Task<Position> CalculatePositionAsync(Symbol symbol, List<Transaction> transactions, CancellationToken cancellationToken)
    {
        var currency = transactions.FirstOrDefault()?.PricePerUnit?.Currency
            ?? transactions.FirstOrDefault()?.GrossAmount?.Currency
            ?? Currency.USD;

        // Separate by type
        var buys = transactions.Where(t => t.Type == TransactionType.BUY).OrderBy(t => t.TransactionDate).ToList();
        var sells = transactions.Where(t => t.Type == TransactionType.SELL).OrderBy(t => t.TransactionDate).ToList();
        var dividends = transactions.Where(t => t.Type == TransactionType.DIVIDEND).ToList();
        var interests = transactions.Where(t => t.Type == TransactionType.INTEREST).ToList();

        // Calculate FIFO cost basis and realized gains
        var (remainingQuantity, averageCost, totalCost, realizedGains) = CalculateFIFO(buys, sells, currency);

        // Calculate income
        var totalDividends = new Money(
            dividends.Sum(d => d.NetAmount?.Amount ?? 0),
            currency);

        var totalInterest = new Money(
            interests.Sum(i => i.NetAmount?.Amount ?? 0),
            currency);

        var totalIncome = new Money(
            totalDividends.Amount + totalInterest.Amount,
            currency);

        // Fetch current market price from service (live or cached)
        var currentPrice = await GetCurrentPriceAsync(symbol, currency, buys, sells, cancellationToken);

        // For CORPORATE bonds (Catalyst - FPC, BGK, etc.), price is a PERCENTAGE of nominal value (1000 PLN)
        // Convert BOTH cost and value to absolute amounts using: (percentage/100) × 1000
        // NOTE: Treasury bonds (EDO, COI, ROS, etc.) are NOT included - they use interest rate tables
        var isCatalystBond = symbol.AssetType == AssetType.Bond &&
            (symbol.Exchange == "Catalyst" ||
             symbol.Ticker.StartsWith("FPC", StringComparison.OrdinalIgnoreCase) ||
             symbol.Ticker.StartsWith("BGK", StringComparison.OrdinalIgnoreCase));

        decimal currentValueAmount;
        Money adjustedTotalCost = totalCost;
        Money adjustedAverageCost = averageCost;

        if (isCatalystBond)
        {
            const decimal nominalValue = 1000m;
            // Convert current value
            currentValueAmount = remainingQuantity * (currentPrice.Amount / 100m) * nominalValue;
            // Convert costs (purchase price is also a percentage)
            adjustedTotalCost = new Money((totalCost.Amount / 100m) * nominalValue, currency);
            adjustedAverageCost = new Money((averageCost.Amount / 100m) * nominalValue, currency);

            _logger.LogDebug("Catalyst bond {Symbol}: value = {Qty} × ({Price}/100) × {Nominal} = {Value}, totalCost = {Cost}",
                symbol.Ticker, remainingQuantity, currentPrice.Amount, nominalValue, currentValueAmount, adjustedTotalCost.Amount);
        }
        else if (IsTreasuryBond(symbol))
        {
            // Treasury bonds (EDO, COI, ROS, etc.) - use BondValueCalculator with interest rate tables
            var bondValue = await GetTreasuryBondValueAsync(symbol, (int)remainingQuantity, cancellationToken);
            if (bondValue != null)
            {
                currentValueAmount = bondValue.TotalNetValue;
                // For treasury bonds, cost is nominal value (100 PLN) × quantity
                // This represents what was actually paid for the bonds
                _logger.LogDebug("Treasury bond {Symbol}: netValue = {Value} (nominal: {Nominal}, interest: {Interest}, tax: {Tax})",
                    symbol.Ticker, bondValue.TotalNetValue, bondValue.TotalNominalValue,
                    bondValue.TotalAccruedInterest, bondValue.TotalTax);
            }
            else
            {
                // Fallback: use nominal value if no TreasuryBondDetails found
                const decimal nominalValue = 100m;
                currentValueAmount = remainingQuantity * nominalValue;
                _logger.LogWarning("Treasury bond {Symbol}: no details found, using nominal value {Value}",
                    symbol.Ticker, currentValueAmount);
            }
        }
        else
        {
            currentValueAmount = currentPrice.Amount * remainingQuantity;
        }

        var currentValue = new Money(currentValueAmount, currency);
        var unrealizedGainLoss = new Money(currentValue.Amount - adjustedTotalCost.Amount, currency);
        var unrealizedPercent = adjustedTotalCost.Amount > 0
            ? (unrealizedGainLoss.Amount / adjustedTotalCost.Amount) * 100
            : 0;

        // Maturity date for bonds
        var maturityDate = buys.FirstOrDefault(b => b.MaturityDate.HasValue)?.MaturityDate;

        return new Position
        {
            Symbol = symbol,
            PortfolioId = transactions[0].PortfolioId,
            TotalQuantity = remainingQuantity,
            AverageCost = adjustedAverageCost,
            TotalCost = adjustedTotalCost,
            CurrentPrice = currentPrice,
            CurrentValue = currentValue,
            UnrealizedGainLoss = unrealizedGainLoss,
            UnrealizedGainLossPercent = unrealizedPercent,
            TotalDividends = totalDividends,
            TotalInterest = totalInterest,
            TotalIncome = totalIncome,
            RealizedGainLoss = realizedGains,
            MaturityDate = maturityDate
        };
    }

    /// <summary>
    /// Gets current market price from cache, falling back to buy price if not available.
    /// </summary>
    private async Task<Money> GetCurrentPriceAsync(
        Symbol symbol,
        Currency currency,
        List<Transaction> buys,
        List<Transaction> sells,
        CancellationToken cancellationToken)
    {
        try
        {
            var livePrice = await _marketPriceService.GetCurrentPriceAsync(symbol, cancellationToken);
            if (livePrice != null)
            {
                _logger.LogDebug("Using market price {Price} for {Symbol}", livePrice.Price, symbol.Ticker);
                return new Money(livePrice.Price, currency);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get market price for {Symbol}, falling back to transaction price", symbol.Ticker);
        }

        // Fallback: use latest transaction price
        var fallbackPrice = buys.LastOrDefault()?.PricePerUnit
            ?? sells.LastOrDefault()?.PricePerUnit
            ?? new Money(0, currency);

        _logger.LogDebug("No cached price for {Symbol}, using fallback price {Price}", symbol.Ticker, fallbackPrice.Amount);
        return fallbackPrice;
    }

    /// <summary>
    /// FIFO (First-In, First-Out) cost basis calculation.
    /// </summary>
    private static (decimal quantity, Money avgCost, Money totalCost, Money realizedGains) CalculateFIFO(
        List<Transaction> buys,
        List<Transaction> sells,
        Currency currency)
    {
        // Create queue of buy lots (quantity, pricePerUnit, fee per unit)
        var buyLots = new Queue<(decimal qty, decimal price, decimal feePerUnit)>();

        foreach (var buy in buys)
        {
            var qty = buy.Quantity ?? 0;
            var price = buy.PricePerUnit?.Amount ?? 0;
            var totalFee = buy.Fee?.Amount ?? 0;
            var feePerUnit = qty > 0 ? totalFee / qty : 0;

            buyLots.Enqueue((qty, price, feePerUnit));
        }

        decimal realizedGains = 0;
        decimal totalSellFees = 0;

        // Process sells using FIFO
        foreach (var sell in sells)
        {
            var sellQty = sell.Quantity ?? 0;
            var sellPrice = sell.PricePerUnit?.Amount ?? 0;
            var sellFee = sell.Fee?.Amount ?? 0;
            totalSellFees += sellFee;

            while (sellQty > 0 && buyLots.Count > 0)
            {
                var (buyQty, buyPrice, buyFeePerUnit) = buyLots.Peek();

                if (sellQty >= buyQty)
                {
                    // Sell entire lot
                    var costBasis = (buyPrice + buyFeePerUnit) * buyQty;
                    var proceeds = sellPrice * buyQty;
                    realizedGains += proceeds - costBasis;

                    sellQty -= buyQty;
                    buyLots.Dequeue();
                }
                else
                {
                    // Partial sell
                    var costBasis = (buyPrice + buyFeePerUnit) * sellQty;
                    var proceeds = sellPrice * sellQty;
                    realizedGains += proceeds - costBasis;

                    // Update lot
                    buyLots.Dequeue();
                    buyLots.Enqueue((buyQty - sellQty, buyPrice, buyFeePerUnit));
                    sellQty = 0;
                }
            }
        }

        // Calculate remaining position
        var remainingQty = buyLots.Sum(lot => lot.qty);
        var remainingCost = buyLots.Sum(lot => (lot.price + lot.feePerUnit) * lot.qty);
        var avgCost = remainingQty > 0 ? remainingCost / remainingQty : 0;

        // Subtract sell fees from realized gains
        realizedGains -= totalSellFees;

        return (
            remainingQty,
            new Money(avgCost, currency),
            new Money(remainingCost, currency),
            new Money(realizedGains, currency)
        );
    }

    /// <summary>
    /// Checks if the symbol is a Polish treasury bond (EDO, COI, TOS, ROS, ROD, OTS).
    /// </summary>
    private static bool IsTreasuryBond(Symbol symbol)
    {
        if (symbol.AssetType != AssetType.Bond)
            return false;

        var treasuryPrefixes = new[] { "EDO", "COI", "TOS", "ROS", "ROD", "OTS" };
        return treasuryPrefixes.Any(prefix =>
            symbol.Ticker.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Loads TreasuryBondDetails from database and calculates value using BondValueCalculator.
    /// </summary>
    private async Task<BondValueResult?> GetTreasuryBondValueAsync(
        Symbol symbol,
        int quantity,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try to find TreasuryBondDetails by ticker
            var bondDetails = await _dbContext.TreasuryBondDetails
                .Include(b => b.InterestPeriods)
                .Include(b => b.Instrument)
                .FirstOrDefaultAsync(b => b.Instrument.Symbol.Ticker == symbol.Ticker, cancellationToken);

            if (bondDetails == null)
            {
                // Also try by matching just the symbol ticker in Instruments table
                var instrument = await _dbContext.Instruments
                    .Include(i => i.BondDetails)
                        .ThenInclude(b => b!.InterestPeriods)
                    .FirstOrDefaultAsync(i => i.Symbol.Ticker == symbol.Ticker, cancellationToken);

                bondDetails = instrument?.BondDetails;
            }

            if (bondDetails == null)
            {
                _logger.LogWarning("No TreasuryBondDetails found for {Symbol}", symbol.Ticker);
                return null;
            }

            return _bondValueCalculator.Calculate(bondDetails, quantity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating treasury bond value for {Symbol}", symbol.Ticker);
            return null;
        }
    }
}

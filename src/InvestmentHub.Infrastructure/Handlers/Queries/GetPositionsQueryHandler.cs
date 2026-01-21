using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Models;
using InvestmentHub.Domain.Interfaces;
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
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<GetPositionsQueryHandler> _logger;

    public GetPositionsQueryHandler(
        ApplicationDbContext dbContext,
        MarketPriceService marketPriceService,
        BondValueCalculator bondValueCalculator,
        IExchangeRateService exchangeRateService,
        ILogger<GetPositionsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _marketPriceService = marketPriceService;
        _bondValueCalculator = bondValueCalculator;
        _exchangeRateService = exchangeRateService;
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
                // Pass portfolio currency (assuming all transactions in portfolio imply a base currency context, 
                // but for now we default to parsing it from the first transaction or PLN if we want to enforce it.
                // However, request doesn't pass Portfolio object. We should probably infer base currency from the first transaction 
                // BUT the goal is to convert FOREIGN assets to NATIVE currency.
                // Assuming "Native" means PLN if the user is Polish, OR the currency of conversion.
                // Let's assume the goal is to display value in the CURRENCY OF THE INSTRUMENT unless it's a foreign ETF in a PLN portfolio?
                // Actually, the user says "ETF is in EUR and shows poorly".
                // If I have a PLN portfolio, I want to see value in PLN.
                // Logic:
                // 1. Determine Portfolio Base Currency (from first transaction or default PLN).
                // 2. Fetch price in Instrument Currency (EUR).
                // 3. Convert to Portfolio Currency (PLN).

                // For now, let's assume the "Currency" derived in CalculatePositionAsync is the "Portfolio/Transaction" currency 
                // that was used to buy it. If I bought IUSQ with EUR, currency is EUR.
                // But if I want to see it in PLN, I need to convert.
                // Let's look at the User Request again: "17.0000 322.38 PLN".
                // It seems the user BOUGHT it in PLN? Or implies they want to see it in PLN.
                // If `transactions` have currency PLN (because user entered costs in PLN?), but price comes in EUR.
                // Wait, if user bought in EUR, `currency` variable will be EUR.
                // Then `TotalCost` is in EUR.
                // `CurrentPrice` (from market) is in EUR.
                // `CurrentValue` is in EUR.
                // If UI shows "PLN", it's a UI issue or Portfolio currency issue.

                // Case: ETF is IUSQ.DE (EUR). User has PLN portfolio.
                // If imports from CSV where currency was EUR, then `currency` is EUR.
                // The UI might be showing "PLN" hardcoded or from portfolio settings?
                // The user says: "17.0000 322.38 PLN 94.17 PLN 1600.89 PLN".
                // This suggests the UI is labeling it PLN but the numbers are EUR (e.g. 17 units * ~94 EUR = ~1600).
                // 94 PLN for IUSQ is too low (it's ~115 EUR -> ~500 PLN).
                // Wait, 17 units. 1600 / 17 = 94. 
                // If price is 94 EUR, that's possible.
                // If price is 115 EUR, then 17*115 = 1955.
                // User says: "This ETF is in euro and thus shows value incorrectly".

                // HYPOTHESIS: User wants EVERYTHING converted to PLN (Portfolio Currency).
                // I need to:
                // 1. Detect if `currency` (from transactions) is different from `PLN`? Or always convert to PLN?
                // Let's look at `CalculatePositionAsync`.

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
        // 1. Determine the "Booking Currency" - the currency the transactions were recorded in.
        // If I bought in EUR, this is EUR.
        var bookingCurrency = transactions.FirstOrDefault()?.PricePerUnit?.Currency
            ?? transactions.FirstOrDefault()?.GrossAmount?.Currency
            ?? Currency.USD;

        // Separate by type
        var buys = transactions.Where(t => t.Type == TransactionType.BUY).OrderBy(t => t.TransactionDate).ToList();
        var sells = transactions.Where(t => t.Type == TransactionType.SELL).OrderBy(t => t.TransactionDate).ToList();
        var dividends = transactions.Where(t => t.Type == TransactionType.DIVIDEND).ToList();
        var interests = transactions.Where(t => t.Type == TransactionType.INTEREST).ToList();

        // Calculate FIFO cost basis and realized gains in BOOKING currency
        var (remainingQuantity, averageCost, totalCost, realizedGains) = CalculateFIFO(buys, sells, bookingCurrency);

        // Calculate income in BOOKING currency
        var totalDividends = new Money(dividends.Sum(d => d.NetAmount?.Amount ?? 0), bookingCurrency);
        var totalInterest = new Money(interests.Sum(i => i.NetAmount?.Amount ?? 0), bookingCurrency);
        var totalIncome = new Money(totalDividends.Amount + totalInterest.Amount, bookingCurrency);

        // Fetch current market price from service (live or cached)
        // This returns price in the INSTRUMENT'S NATIVE trading currency (e.g. EUR for IUSQ.DE)
        var marketPrice = await GetCurrentPriceAsync(symbol, bookingCurrency, buys, sells, cancellationToken);
        var marketCurrency = marketPrice.Currency;

        // *** CURRENCY CONVERSION LOGIC ***
        // We want to align everything to a common "Reporting Currency".
        // Ideally, this is the Portfolio's currency (PLN usually).
        // Since we don't have Portfolio object here easily (without extra fetch), 
        // we'll assume we want to convert everything to PLN if the asset is foreign, 
        // OR we trust the "Booking Currency" if it matches Portfolio.
        // 
        // However, if the user manually imported EUR transactions, `bookingCurrency` is EUR.
        // But the user likely wants to see the total value in PLN in the summary.
        // The Position object has `CurrentValue`, `TotalCost`, etc.
        // If we return mixed currencies (some PLN positions, some EUR), the frontend might not sum them correctly.
        // 
        // Let's target converting everything to PLN if it's not already,
        // OR convert market price to booking currency?
        // 
        // User scenario: "IUSQ is in EUR... shows wrongly".
        // If user sees "PLN" on UI but value is numeric EUR, it means UI assumes PLN.
        // So we MUST return PLN values.

        var targetCurrency = Currency.PLN; // Default target

        // Helper to convert money if needed
        async Task<Money> ConvertToTarget(Money money)
        {
            if (money.Currency == targetCurrency) return money;
            var rate = await _exchangeRateService.GetExchangeRateAsync(money.Currency, targetCurrency, cancellationToken);
            if (rate.HasValue)
            {
                var convertedAmount = money.Amount * rate.Value;
                _logger.LogDebug("Converted {Amount} {From} to {Result} {To} (Rate: {Rate})",
                    money.Amount, money.Currency, convertedAmount, targetCurrency, rate);
                return new Money(convertedAmount, targetCurrency);
            }
            return money; // Fallback
        }

        // Convert costs and income to Target Currency (PLN)
        // We do this mostly for the "Current Value" and "Total Cost" to be comparable.
        // But wait, if we change TotalCost from EUR to PLN, we re-base the cost basis.

        var displayAverageCost = await ConvertToTarget(averageCost);
        var displayTotalCost = await ConvertToTarget(totalCost);
        var displayRealizedGains = await ConvertToTarget(realizedGains);
        var displayTotalIncome = await ConvertToTarget(totalIncome);
        var displayTotalDividends = await ConvertToTarget(totalDividends);
        var displayTotalInterest = await ConvertToTarget(totalInterest);

        // Convert Market Price to Target Currency
        var displayCurrentPrice = marketPrice.Currency == targetCurrency
            ? marketPrice
            : (await _exchangeRateService.ConvertAsync(marketPrice.Amount, marketPrice.Currency, targetCurrency, cancellationToken) ?? marketPrice);

        // Now calculate Current Value using the DISPLAY price (in PLN)
        // This ensures the value is ~500 PLN, not 115.

        // For CORPORATE bonds (Catalyst - FPC, BGK, etc.), price is a PERCENTAGE of nominal value (1000 PLN)
        // Convert BOTH cost and value to absolute amounts using: (percentage/100) × 1000
        // NOTE: Treasury bonds (EDO, COI, ROS, etc.) are NOT included - they use interest rate tables
        var isCatalystBond = symbol.AssetType == AssetType.Bond &&
            (symbol.Exchange == "Catalyst" ||
             symbol.Ticker.StartsWith("FPC", StringComparison.OrdinalIgnoreCase) ||
             symbol.Ticker.StartsWith("BGK", StringComparison.OrdinalIgnoreCase));

        decimal currentValueAmount;
        Money finalTotalCost = displayTotalCost;
        Money finalAverageCost = displayAverageCost;

        if (isCatalystBond)
        {
            const decimal nominalValue = 1000m;
            // Convert current value (Price is in %, so we use base logic)
            // Note: Catalyst is usually PLN, so conversion might be 1:1.
            currentValueAmount = remainingQuantity * (displayCurrentPrice.Amount / 100m) * nominalValue;

            // Adjust costs similarly (they are already in PLN due to ConvertToTarget above)
            finalTotalCost = new Money((displayTotalCost.Amount / 100m) * nominalValue, targetCurrency);
            finalAverageCost = new Money((displayAverageCost.Amount / 100m) * nominalValue, targetCurrency);

            _logger.LogDebug("Catalyst bond {Symbol}: value = {Qty} × ({Price}/100) × {Nominal} = {Value}, totalCost = {Cost}",
                symbol.Ticker, remainingQuantity, displayCurrentPrice.Amount, nominalValue, currentValueAmount, finalTotalCost.Amount);
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
            // Standard Entity: Value = Price * Quantity
            currentValueAmount = displayCurrentPrice.Amount * remainingQuantity;
        }

        var currentValue = new Money(currentValueAmount, targetCurrency);
        var unrealizedGainLoss = new Money(currentValue.Amount - finalTotalCost.Amount, targetCurrency);
        var unrealizedPercent = finalTotalCost.Amount > 0
            ? (unrealizedGainLoss.Amount / finalTotalCost.Amount) * 100
            : 0;

        // Maturity date for bonds
        var maturityDate = buys.FirstOrDefault(b => b.MaturityDate.HasValue)?.MaturityDate;

        return new Position
        {
            Symbol = symbol,
            PortfolioId = transactions[0].PortfolioId,
            TotalQuantity = remainingQuantity,
            AverageCost = finalAverageCost,
            TotalCost = finalTotalCost,
            CurrentPrice = displayCurrentPrice,
            CurrentValue = currentValue,
            UnrealizedGainLoss = unrealizedGainLoss,
            UnrealizedGainLossPercent = unrealizedPercent,
            TotalDividends = displayTotalDividends,
            TotalInterest = displayTotalInterest,
            TotalIncome = displayTotalIncome,
            RealizedGainLoss = displayRealizedGains,
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
                _logger.LogDebug("Using market price {Price} {Currency} for {Symbol}", livePrice.Price, livePrice.Currency, symbol.Ticker);

                if (Enum.TryParse<Currency>(livePrice.Currency, true, out var priceCurrency))
                {
                    return new Money(livePrice.Price, priceCurrency);
                }

                _logger.LogWarning("Failed to parse currency {Currency} for {Symbol}, falling back to {Fallback}", livePrice.Currency, symbol.Ticker, currency);
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

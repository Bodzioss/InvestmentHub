using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.Models;
using InvestmentHub.Domain.Queries;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Infrastructure.Data;
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
    private readonly ILogger<GetPositionsQueryHandler> _logger;

    public GetPositionsQueryHandler(ApplicationDbContext dbContext, ILogger<GetPositionsQueryHandler> logger)
    {
        _dbContext = dbContext;
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
                var position = CalculatePosition(symbol, group.ToList());
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
    private Position CalculatePosition(Symbol symbol, List<Transaction> transactions)
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

        // For current price, use latest transaction price or zero
        var latestPrice = buys.LastOrDefault()?.PricePerUnit
            ?? sells.LastOrDefault()?.PricePerUnit
            ?? new Money(0, currency);

        var currentValue = new Money(latestPrice.Amount * remainingQuantity, currency);
        var unrealizedGainLoss = new Money(currentValue.Amount - totalCost.Amount, currency);
        var unrealizedPercent = totalCost.Amount > 0
            ? (unrealizedGainLoss.Amount / totalCost.Amount) * 100
            : 0;

        // Maturity date for bonds
        var maturityDate = buys.FirstOrDefault(b => b.MaturityDate.HasValue)?.MaturityDate;

        return new Position
        {
            Symbol = symbol,
            PortfolioId = transactions.First().PortfolioId,
            TotalQuantity = remainingQuantity,
            AverageCost = averageCost,
            TotalCost = totalCost,
            CurrentPrice = latestPrice,
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
    /// FIFO (First-In, First-Out) cost basis calculation.
    /// </summary>
    private (decimal quantity, Money avgCost, Money totalCost, Money realizedGains) CalculateFIFO(
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
}

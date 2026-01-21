using InvestmentHub.Domain.Aggregates;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;

namespace InvestmentHub.Infrastructure.Services;

/// <summary>
/// FIFO (First-In, First-Out) cost basis calculator for positions.
/// Extracted for testability.
/// </summary>
public static class FIFOCalculator
{
    /// <summary>
    /// Calculate FIFO cost basis from buy/sell transactions.
    /// </summary>
    /// <param name="buys">List of buy transactions ordered by date</param>
    /// <param name="sells">List of sell transactions ordered by date</param>
    /// <param name="currency">Currency for money values</param>
    /// <returns>Tuple containing remaining quantity, average cost, total cost, and realized gains</returns>
    public static (decimal RemainingQuantity, Money AverageCost, Money TotalCost, Money RealizedGains) Calculate(
        List<Transaction> buys,
        List<Transaction> sells,
        Currency currency)
    {
        // Create list of buy lots (quantity, pricePerUnit, fee per unit) - using List for proper FIFO
        var buyLots = new List<(decimal qty, decimal price, decimal feePerUnit)>();

        foreach (var buy in buys)
        {
            var qty = buy.Quantity ?? 0;
            var price = buy.PricePerUnit?.Amount ?? 0;
            var totalFee = buy.Fee?.Amount ?? 0;
            var feePerUnit = qty > 0 ? totalFee / qty : 0;

            buyLots.Add((qty, price, feePerUnit));
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
                var (buyQty, buyPrice, buyFeePerUnit) = buyLots[0];

                if (sellQty >= buyQty)
                {
                    // Sell entire lot
                    var costBasis = (buyPrice + buyFeePerUnit) * buyQty;
                    var proceeds = sellPrice * buyQty;
                    realizedGains += proceeds - costBasis;

                    sellQty -= buyQty;
                    buyLots.RemoveAt(0);
                }
                else
                {
                    // Partial sell - update the FIRST lot in place
                    var costBasis = (buyPrice + buyFeePerUnit) * sellQty;
                    var proceeds = sellPrice * sellQty;
                    realizedGains += proceeds - costBasis;

                    // Update lot at index 0 (keep at front for proper FIFO)
                    buyLots[0] = (buyQty - sellQty, buyPrice, buyFeePerUnit);
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
    /// Simplified FIFO calculation from raw data (for unit testing).
    /// </summary>
    public static (decimal RemainingQuantity, decimal AverageCost, decimal TotalCost, decimal RealizedGains) CalculateSimple(
        List<(decimal quantity, decimal price, decimal fee)> buys,
        List<(decimal quantity, decimal price, decimal fee)> sells)
    {
        var buyLots = new List<(decimal qty, decimal price, decimal feePerUnit)>();

        foreach (var (qty, price, fee) in buys)
        {
            var feePerUnit = qty > 0 ? fee / qty : 0;
            buyLots.Add((qty, price, feePerUnit));
        }

        decimal realizedGains = 0;
        decimal totalSellFees = 0;

        foreach (var (sellQtyOriginal, sellPrice, sellFee) in sells)
        {
            var sellQty = sellQtyOriginal;
            totalSellFees += sellFee;

            while (sellQty > 0 && buyLots.Count > 0)
            {
                var (buyQty, buyPrice, buyFeePerUnit) = buyLots[0];

                if (sellQty >= buyQty)
                {
                    var costBasis = (buyPrice + buyFeePerUnit) * buyQty;
                    var proceeds = sellPrice * buyQty;
                    realizedGains += proceeds - costBasis;

                    sellQty -= buyQty;
                    buyLots.RemoveAt(0);
                }
                else
                {
                    var costBasis = (buyPrice + buyFeePerUnit) * sellQty;
                    var proceeds = sellPrice * sellQty;
                    realizedGains += proceeds - costBasis;

                    // Update lot at index 0 (keep at front for proper FIFO)
                    buyLots[0] = (buyQty - sellQty, buyPrice, buyFeePerUnit);
                    sellQty = 0;
                }
            }
        }

        var remainingQty = buyLots.Sum(lot => lot.qty);
        var remainingCost = buyLots.Sum(lot => (lot.price + lot.feePerUnit) * lot.qty);
        var avgCost = remainingQty > 0 ? remainingCost / remainingQty : 0;

        realizedGains -= totalSellFees;

        return (remainingQty, avgCost, remainingCost, realizedGains);
    }
}

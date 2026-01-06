using InvestmentHub.Domain.Enums;
using System.Globalization;

namespace InvestmentHub.Infrastructure.Services;

/// <summary>
/// Parses CSV files exported from MyFund.pl
/// </summary>
public class MyFundCsvParser
{
    /// <summary>
    /// Represents a parsed transaction from MyFund CSV
    /// </summary>
    public record ParsedTransaction
    {
        public DateTime Date { get; init; }
        public string OperationType { get; init; } = string.Empty;
        public string Account { get; init; } = string.Empty;
        public string Ticker { get; init; } = string.Empty;
        public Currency Currency { get; init; }
        public decimal Quantity { get; init; }
        public decimal PricePerUnit { get; init; }
        public decimal TotalValue { get; init; }
        public string? Notes { get; init; }

        /// <summary>
        /// Maps Polish operation type to TransactionType enum
        /// </summary>
        public TransactionType? GetTransactionType()
        {
            var op = OperationType.ToLowerInvariant();

            if (op.Contains("kupno")) return TransactionType.BUY;
            if (op.Contains("sprzeda")) return TransactionType.SELL; // "sprzedaż" - resilient to 'ż' encoding
            if (op.Contains("odsetki")) return TransactionType.INTEREST;
            if (op.Contains("dywidenda")) return TransactionType.DIVIDEND;

            return null;
        }

        public bool IsInvestmentTransaction => GetTransactionType() != null;
    }

    public record ParseResult
    {
        public List<ParsedTransaction> Transactions { get; init; } = new();
        public List<string> Errors { get; init; } = new();
        public List<string> Warnings { get; init; } = new();
        public int TotalRows { get; init; }
        public int SkippedRows { get; init; }
        public bool IsSuccess => Errors.Count == 0;
    }

    private static readonly Dictionary<string, Currency> CurrencyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "PLN", Currency.PLN },
        { "USD", Currency.USD },
        { "EUR", Currency.EUR },
        { "GBP", Currency.GBP },
        { "CHF", Currency.CHF }
    };

    /// <summary>
    /// Parses MyFund CSV content
    /// </summary>
    /// <param name="csvContent">CSV file content as string</param>
    /// <returns>Parse result with transactions and any errors</returns>
    public ParseResult Parse(string csvContent)
    {
        var result = new ParseResult();
        var transactions = new List<ParsedTransaction>();
        var errors = new List<string>();
        var warnings = new List<string>();
        int totalRows = 0;
        int skippedRows = 0;

        var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            return result with { Errors = new List<string> { "Plik CSV jest pusty" } };
        }

        // Skip header row
        var headerLine = lines[0];
        var headers = ParseCsvLine(headerLine);

        // Validate expected headers
        var headerValidation = ValidateHeaders(headers);
        if (headerValidation != null)
        {
            return result with { Errors = new List<string> { headerValidation } };
        }

        // Find column indices
        var columnMap = CreateColumnMap(headers);

        for (int i = 1; i < lines.Length; i++)
        {
            totalRows++;
            var line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                skippedRows++;
                continue;
            }

            try
            {
                var columns = ParseCsvLine(line);
                var transaction = ParseRow(columns, columnMap);

                if (transaction == null)
                {
                    skippedRows++;
                    continue;
                }

                if (!transaction.IsInvestmentTransaction)
                {
                    warnings.Add($"Wiersz {i + 1}: Pominięto operację '{transaction.OperationType}' (nie jest transakcją inwestycyjną)");
                    skippedRows++;
                    continue;
                }

                transactions.Add(transaction);
            }
            catch (Exception ex)
            {
                errors.Add($"Wiersz {i + 1}: {ex.Message}");
            }
        }

        return new ParseResult
        {
            Transactions = transactions,
            Errors = errors,
            Warnings = warnings,
            TotalRows = totalRows,
            SkippedRows = skippedRows
        };
    }

    private static string[] ParseCsvLine(string line)
    {
        // MyFund uses semicolon as delimiter
        return line.Split(';').Select(s => s.Trim()).ToArray();
    }

    private static string? ValidateHeaders(string[] headers)
    {
        var requiredHeaders = new[] { "Data", "Operacja", "Walor", "Waluta" };
        var missingHeaders = requiredHeaders.Where(h =>
            !headers.Any(header => header.Equals(h, StringComparison.OrdinalIgnoreCase))).ToList();

        if (missingHeaders.Count > 0)
        {
            return $"Brakujące nagłówki: {string.Join(", ", missingHeaders)}";
        }

        return null;
    }

    private static Dictionary<string, int> CreateColumnMap(string[] headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            map[headers[i]] = i;
        }
        return map;
    }

    private ParsedTransaction? ParseRow(string[] columns, Dictionary<string, int> columnMap)
    {
        string GetValue(string columnName) =>
            columnMap.TryGetValue(columnName, out var index) && index < columns.Length
                ? columns[index]
                : string.Empty;

        var dateStr = GetValue("Data");
        var operation = GetValue("Operacja");
        var account = GetValue("Konto");
        var ticker = GetValue("Walor");
        var currencyStr = GetValue("Waluta");
        var quantityStr = GetValue("Liczba jednostek");
        var priceStr = GetValue("Cena");
        var valueStr = GetValue("Prowizja dla kupna");
        var notes = GetValue("Komentarz");

        // Skip if no date or operation
        if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(operation))
        {
            return null;
        }

        // Parse date
        if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new FormatException($"Nieprawidłowy format daty: {dateStr}");
        }

        // Parse currency
        if (!CurrencyMap.TryGetValue(currencyStr, out var currency))
        {
            currency = Currency.PLN; // Default to PLN
        }

        // Parse quantity
        decimal quantity = 0;
        if (!string.IsNullOrWhiteSpace(quantityStr))
        {
            quantityStr = quantityStr.Replace(',', '.').Replace(" ", "");
            decimal.TryParse(quantityStr, NumberStyles.Any, CultureInfo.InvariantCulture, out quantity);
        }

        // Parse price per unit
        decimal pricePerUnit = 0;
        if (!string.IsNullOrWhiteSpace(priceStr))
        {
            priceStr = priceStr.Replace(',', '.').Replace(" ", "").Replace("zł", "").Trim();
            decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out pricePerUnit);
        }

        // Parse total value if available
        decimal totalValue = quantity * pricePerUnit;
        if (!string.IsNullOrWhiteSpace(valueStr))
        {
            valueStr = valueStr.Replace(',', '.').Replace(" ", "").Replace("zł", "").Trim();
            decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out totalValue);
        }

        // Extract ticker from "Walor" (e.g., "CDPROJECT (CDR)" -> "CDR")
        var extractedTicker = ExtractTicker(ticker);

        return new ParsedTransaction
        {
            Date = date,
            OperationType = operation,
            Account = account,
            Ticker = extractedTicker,
            Currency = currency,
            Quantity = Math.Abs(quantity),
            PricePerUnit = Math.Abs(pricePerUnit),
            TotalValue = Math.Abs(totalValue),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes
        };
    }

    private static string ExtractTicker(string walor)
    {
        if (string.IsNullOrWhiteSpace(walor))
            return string.Empty;

        // Pattern: "NAZWA (TICKER)" - extract TICKER from parentheses
        // Use RightToLeft to find the last occurrence (e.g. "ETF (Dist) (VHYL.AS)" -> "VHYL.AS")
        var match = System.Text.RegularExpressions.Regex.Match(walor, @"\(([^)]+)\)", System.Text.RegularExpressions.RegexOptions.RightToLeft);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // If no parentheses, use the whole string as ticker
        return walor.Trim();
    }
}

using MediatR;
using Microsoft.AspNetCore.Mvc;
using InvestmentHub.Domain.Commands;
using InvestmentHub.Domain.ValueObjects;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Contracts.Import;
using InvestmentHub.Infrastructure.Services;
using InvestmentHub.Infrastructure.Data;

namespace InvestmentHub.API.Controllers;

/// <summary>
/// Controller for importing transactions from external sources
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ImportController> _logger;
    private readonly MyFundCsvParser _csvParser;
    private readonly ApplicationDbContext _context;

    public ImportController(
        IMediator mediator,
        ILogger<ImportController> logger,
        MyFundCsvParser csvParser,
        ApplicationDbContext context)
    {
        _mediator = mediator;
        _logger = logger;
        _csvParser = csvParser;
        _context = context;
    }

    /// <summary>
    /// Uploads and parses a MyFund CSV file for preview
    /// </summary>
    /// <param name="file">CSV file from MyFund.pl export</param>
    /// <returns>Parsed transactions for preview before import</returns>
    [HttpPost("myfund/preview")]
    [ProducesResponseType(typeof(ImportPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportPreviewResponse>> PreviewMyFundCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ImportPreviewResponse
            {
                Errors = new List<string> { "Nie przesłano pliku" }
            });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ImportPreviewResponse
            {
                Errors = new List<string> { "Plik musi mieć rozszerzenie .csv" }
            });
        }

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var csvContent = await reader.ReadToEndAsync();

            var parseResult = _csvParser.Parse(csvContent);

            var tickers = parseResult.Transactions.Select(t => t.Ticker).Distinct().ToList();
            var existingInstruments = _context.Instruments
                .Where(i => tickers.Contains(i.Symbol.Ticker))
                .Select(i => new { i.Symbol.Ticker, i.Symbol.AssetType })
                .AsEnumerable()
                .ToDictionary(i => i.Ticker, i => i.AssetType);

            var response = new ImportPreviewResponse
            {
                Transactions = parseResult.Transactions.Select(t =>
                {
                    var exists = existingInstruments.TryGetValue(t.Ticker, out var assetType);
                    return new ParsedTransactionDto
                    {
                        Date = t.Date,
                        OperationType = t.OperationType,
                        TransactionType = t.GetTransactionType()?.ToString() ?? "Unknown",
                        Account = t.Account,
                        Ticker = t.Ticker,
                        Currency = t.Currency.ToString(),
                        Quantity = t.Quantity,
                        PricePerUnit = t.PricePerUnit,
                        TotalValue = t.TotalValue,
                        Notes = t.Notes,
                        InstrumentExists = exists,
                        AssetType = exists ? assetType.ToString() : null,
                        Selected = exists // Only select by default if it exists
                    };
                }).ToList(),
                Errors = parseResult.Errors,
                Warnings = parseResult.Warnings,
                TotalRows = parseResult.TotalRows,
                SkippedRows = parseResult.SkippedRows
            };

            _logger.LogInformation(
                "Parsed MyFund CSV: {TransactionCount} transactions from {TotalRows} rows",
                response.ParsedTransactions,
                response.TotalRows);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing MyFund CSV file");
            return BadRequest(new ImportPreviewResponse
            {
                Errors = new List<string> { $"Błąd parsowania pliku: {ex.Message}" }
            });
        }
    }

    /// <summary>
    /// Imports confirmed transactions into a portfolio
    /// </summary>
    /// <param name="request">Import request with portfolio ID and transactions to import</param>
    /// <returns>Import result with success/failure counts</returns>
    [HttpPost("myfund/import")]
    [ProducesResponseType(typeof(ImportTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportTransactionsResponse>> ImportTransactions(
        [FromBody] ImportTransactionsRequest request)
    {
        if (request.Transactions == null || request.Transactions.Count == 0)
        {
            return BadRequest(new ImportTransactionsResponse
            {
                Errors = new List<string> { "Brak transakcji do zaimportowania" }
            });
        }

        var importedCount = 0;
        var failedCount = 0;
        var errors = new List<string>();
        var portfolioId = new PortfolioId(request.PortfolioId);

        var sortedTransactions = request.Transactions.OrderBy(t => t.Date).ToList();

        foreach (var tx in sortedTransactions)
        {
            try
            {
                if (!Enum.TryParse<TransactionType>(tx.TransactionType, true, out var transactionType))
                {
                    errors.Add($"Nieznany typ transakcji: {tx.TransactionType} dla {tx.Ticker}");
                    failedCount++;
                    continue;
                }

                if (!Enum.TryParse<Currency>(tx.Currency, true, out var currency))
                {
                    currency = Currency.PLN;
                }

                var instrument = _context.Instruments.FirstOrDefault(i => i.Symbol.Ticker == tx.Ticker);
                if (instrument == null)
                {
                    errors.Add($"Instrument {tx.Ticker} nie został znaleziony w systemie. Pomiń go lub dodaj ręcznie.");
                    failedCount++;
                    continue;
                }

                var symbol = new Symbol(tx.Ticker, request.Exchange, instrument.Symbol.AssetType);
                var pricePerUnit = new Money(tx.PricePerUnit, currency);

                switch (transactionType)
                {
                    case TransactionType.BUY:
                        var buyCommand = new RecordBuyTransactionCommand(
                            portfolioId,
                            symbol,
                            tx.Quantity,
                            pricePerUnit,
                            tx.Date,
                            notes: tx.Notes);

                        var buyResult = await _mediator.Send(buyCommand);
                        if (buyResult.IsSuccess)
                            importedCount++;
                        else
                        {
                            errors.Add($"Błąd importu kupna {tx.Ticker}: {buyResult.ErrorMessage}");
                            failedCount++;
                        }
                        break;

                    case TransactionType.SELL:
                        var sellCommand = new RecordSellTransactionCommand(
                            portfolioId,
                            symbol,
                            tx.Quantity,
                            pricePerUnit,
                            tx.Date,
                            notes: tx.Notes);

                        var sellResult = await _mediator.Send(sellCommand);
                        if (sellResult.IsSuccess)
                            importedCount++;
                        else
                        {
                            errors.Add($"Błąd importu sprzedaży {tx.Ticker}: {sellResult.ErrorMessage}");
                            failedCount++;
                        }
                        break;

                    case TransactionType.DIVIDEND:
                        var dividendCommand = new RecordDividendTransactionCommand(
                            portfolioId,
                            symbol,
                            new Money(tx.Quantity * tx.PricePerUnit, currency), // grossAmount
                            tx.Date,
                            taxRate: null,
                            notes: tx.Notes);

                        var dividendResult = await _mediator.Send(dividendCommand);
                        if (dividendResult.IsSuccess)
                            importedCount++;
                        else
                        {
                            errors.Add($"Błąd importu dywidendy {tx.Ticker}: {dividendResult.ErrorMessage}");
                            failedCount++;
                        }
                        break;

                    case TransactionType.INTEREST:
                        var interestCommand = new RecordInterestTransactionCommand(
                            portfolioId,
                            symbol,
                            new Money(tx.Quantity * tx.PricePerUnit, currency), // grossAmount
                            tx.Date,
                            taxRate: null,
                            notes: tx.Notes);

                        var interestResult = await _mediator.Send(interestCommand);
                        if (interestResult.IsSuccess)
                            importedCount++;
                        else
                        {
                            errors.Add($"Błąd importu odsetek {tx.Ticker}: {interestResult.ErrorMessage}");
                            failedCount++;
                        }
                        break;

                    default:
                        errors.Add($"Typ transakcji {transactionType} nie jest jeszcze obsługiwany w trybie automatycznym. Pomiń go lub dodaj ręcznie.");
                        failedCount++;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing transaction for {Ticker}", tx.Ticker);
                errors.Add($"Błąd importu {tx.Ticker}: {ex.Message}");
                failedCount++;
            }
        }

        _logger.LogInformation(
            "Import completed: {Imported} imported, {Failed} failed",
            importedCount,
            failedCount);

        return Ok(new ImportTransactionsResponse
        {
            ImportedCount = importedCount,
            FailedCount = failedCount,
            Errors = errors
        });
    }
}

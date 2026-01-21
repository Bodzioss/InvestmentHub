using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions; // Add this
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using YahooQuotesApi;

namespace InvestmentHub.Infrastructure.Data;

public partial class InstrumentImporter
{
    private readonly ApplicationDbContext _context;
    private readonly YahooQuotes _yahooQuotes;
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    [GeneratedRegex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)")]
    private static partial System.Text.RegularExpressions.Regex CsvRegex();

    public InstrumentImporter(ApplicationDbContext context, YahooQuotes yahooQuotes)
    {
        _context = context;
        _yahooQuotes = yahooQuotes;
    }

    public async Task ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File not found: {filePath}");
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<RootObject>(json);

        if (data?.Rows == null)
        {
            Console.WriteLine("Failed to deserialize data.");
            return;
        }

        var instruments = new List<Instrument>();
        var existingIsins = await _context.Instruments.Select(i => i.Isin).ToListAsync();
        var existingIsinSet = new HashSet<string>(existingIsins);

        foreach (var item in data.Rows)
        {
            if (string.IsNullOrWhiteSpace(item.Isin) || existingIsinSet.Contains(item.Isin))
                continue;

            try
            {
                var (assetType, exchange) = MapGroupToTypeAndExchange(item.Group);

                // Skip if mapping failed (unknown group)
                if (string.IsNullOrEmpty(exchange))
                    continue;

                var symbol = new Domain.ValueObjects.Symbol(item.ShortName, exchange, assetType);
                // Ensure ISIN is max 20 chars
                var safeIsin = item.Isin?.Trim();
                if (safeIsin != null && safeIsin.Length > 20) safeIsin = safeIsin[..20];

                var instrument = new Instrument(symbol, item.Name, safeIsin ?? string.Empty);

                instruments.Add(instrument);
                if (item.Isin != null) existingIsinSet.Add(item.Isin);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing item {item.Name} ({item.Isin}): {ex.Message}");
            }
        }

        if (instruments.Count > 0)
        {
            await _context.Instruments.AddRangeAsync(instruments);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Imported {instruments.Count} instruments.");
        }
        else
        {
            Console.WriteLine("No new instruments to import.");
        }
    }

    public async Task<int> SyncWithYahooAsync(string inputPath, string outputPath)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file not found", inputPath);
        }

        var json = await File.ReadAllTextAsync(inputPath);
        var data = JsonSerializer.Deserialize<RootObject>(json);

        if (data?.Rows == null) return 0;

        var validRows = new List<InstrumentItem>();
        var batchSize = 100;
        var chunks = data.Rows.Chunk(batchSize).ToList();

        Console.WriteLine($"Processing {data.Rows.Count} instruments in {chunks.Count} batches...");

        foreach (var chunk in chunks)
        {
            var symbolMap = new Dictionary<string, InstrumentItem>();

            foreach (var item in chunk)
            {
                // Map GPW/NewConnect to Yahoo format (Ticker.WA)
                var yahooTicker = MapToYahooTicker(item);
                if (!string.IsNullOrEmpty(yahooTicker))
                {
                    symbolMap[yahooTicker] = item;
                }
            }

            try
            {
                // YahooQuotesApi typically supports GetSnapshotAsync for single items.
                // We'll process the batch in parallel or sequence.
                var tasks = symbolMap.Keys.Select(async ticker =>
                {
                    try
                    {
                        var security = await _yahooQuotes.GetSnapshotAsync(ticker);
                        return new { Ticker = ticker, IsFound = security != null };
                    }
                    catch
                    {
                        return new { Ticker = ticker, IsFound = false };
                    }
                });

                var batchResults = await Task.WhenAll(tasks);

                foreach (var batchItem in batchResults)
                {
                    if (batchItem.IsFound && symbolMap.TryGetValue(batchItem.Ticker, out var item))
                    {
                        validRows.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing batch: {ex.Message}");
            }

            // Respect rate limits gently
            await Task.Delay(500);
        }

        // Save valid list
        var result = new RootObject { Rows = validRows };
        var resultJson = JsonSerializer.Serialize(result, IndentedOptions);
        await File.WriteAllTextAsync(outputPath, resultJson);

        return validRows.Count;
    }

    private static string MapToYahooTicker(InstrumentItem item)
    {
        // 1. Get Exchange mapping
        var (_, exchange) = MapGroupToTypeAndExchange(item.Group);

        if (exchange == "GPW" || exchange == "NewConnect" || exchange == "Catalyst")
        {
            return $"{item.ShortName}.WA";
        }

        // For GlobalConnect or others, might need adjustments
        // Assuming Ticker is enough for US stocks if we had them, 
        // but this file seems centered on GPW context based on groups.
        return item.ShortName;
    }

    private static (AssetType Type, string Exchange) MapGroupToTypeAndExchange(string group)
    {
        return group switch
        {
            "01" or "02" or "03" or "10" or "20" => (AssetType.Stock, "GPW"),
            "30" => (AssetType.Stock, "GlobalConnect"),
            "40" or "45" or "48" => (AssetType.Stock, "NewConnect"),
            "60" or "70" => (AssetType.Bond, "Catalyst"),
            "95" or "96" or "97" or "98" => (AssetType.Stock, "GPW"), // Alerts/Other
            _ => (AssetType.Stock, "") // Unknown
        };
    }

    public async Task<int> SyncGlobalWithYahooAsync(string inputPath, string outputPath)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file not found", inputPath);
        }

        var json = await File.ReadAllTextAsync(inputPath);
        var items = JsonSerializer.Deserialize<List<GlobalInstrumentItem>>(json);

        if (items == null || items.Count == 0) return 0;

        var validRows = new List<GlobalInstrumentItem>();
        var batchSize = 100;
        var chunks = items.Chunk(batchSize).ToList();

        Console.WriteLine($"Processing {items.Count} Global instruments in {chunks.Count} batches...");

        foreach (var chunk in chunks)
        {
            var symbolMap = new Dictionary<string, GlobalInstrumentItem>();

            foreach (var item in chunk)
            {
                // NASDAQ Symbol is usually the ticker.
                // Replace special chars if needed (e.g. ^ to - or . depending on Yahoo).
                // But typically Yahoo uses '-' for preferreds like 'BAC-PL'. 
                // The file has 'NASDAQ Symbol' which might check out.
                var ticker = item.NasdaqSymbol?.Replace("^", "-").Replace("/", "-");

                if (!string.IsNullOrWhiteSpace(ticker))
                {
                    symbolMap[ticker] = item;
                }
            }

            try
            {
                var tasks = symbolMap.Keys.Select(async ticker =>
                {
                    try
                    {
                        var security = await _yahooQuotes.GetSnapshotAsync(ticker);
                        return new { Ticker = ticker, IsFound = security != null };
                    }
                    catch
                    {
                        return new { Ticker = ticker, IsFound = false };
                    }
                });

                var batchResults = await Task.WhenAll(tasks);

                foreach (var batchItem in batchResults)
                {
                    if (batchItem.IsFound && symbolMap.TryGetValue(batchItem.Ticker, out var item))
                    {
                        validRows.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing batch: {ex.Message}");
            }

            await Task.Delay(500);
        }

        // Save valid list
        var resultJson = JsonSerializer.Serialize(validRows, IndentedOptions);
        await File.WriteAllTextAsync(outputPath, resultJson);

        return validRows.Count;
    }

    public async Task ImportEtfsCsvAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ETF file not found: {filePath}");
            return;
        }

        var lines = await File.ReadAllLinesAsync(filePath);
        if (lines.Length < 2) return;

        var instruments = new List<Instrument>();
        var etfDetailsList = new List<Domain.Entities.EtfDetails>();
        var existingTickers = new HashSet<string>(await _context.Instruments.Select(i => i.Symbol.Ticker).ToListAsync());

        // Simple CSV parser for quoted fields
        var csvRegex = CsvRegex();

        // Find the header row (contains "Ticker" and "ISIN") - handles multi-line headers
        int headerRowIndex = 0;
        for (int h = 0; h < Math.Min(20, lines.Length); h++)
        {
            if (lines[h].Contains("Ticker") && lines[h].Contains("ISIN"))
            {
                headerRowIndex = h;
                break;
            }
        }
        Console.WriteLine($"ETF CSV: Found header at row {headerRowIndex}, starting data import...");

        // CSV Columns (correct mapping):
        // 0: Rok dodania
        // 1: Ticker (e.g., "XMKA")
        // 2: Ticker (Google) -> e.g. "FRA:XMKA"
        // 3: Kod ISIN
        // 4: Kraj lub region
        // 5: Temat inwestycji
        // 6: Pełna nazwa
        // 7: Zarządzany przez
        // 8: Typ (Accumulating/Distributing)
        // 9: Rok powstania
        // 10: Rezydentura (domicile)
        // 11: Replikacja
        // 12-14: mBank/XTB/BOŚ availability
        // 15: Opłata roczna
        // 16: Aktywa (mln EUR)
        // 17: Liczba instrumentów
        // 18: Waluta funduszu

        for (int i = headerRowIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = csvRegex.Split(line).Select(p => p.Trim('"')).ToArray();
            if (parts.Length < 7) continue; // Need at least through Name column

            try
            {
                // Parse ISIN (col 3)
                var isin = parts.Length > 3 ? parts[3].Trim() : "";
                if (string.IsNullOrEmpty(isin)) continue;
                if (isin.Length > 20) isin = isin[..20]; // Truncate to DB limit

                // Parse ticker from column 1 (simple ticker like "XMKA")
                var ticker = parts[1].Trim();
                if (string.IsNullOrEmpty(ticker)) continue;
                if (ticker.Contains("IUSQ"))
                {
                    Console.WriteLine($"Skipping IUSQ: {ticker}");
                }
                // Parse Google ticker (col 2) for exchange, e.g., "FRA:XMKA"
                var googleTicker = parts.Length > 2 ? parts[2].Trim() : "";
                var tickerParts = googleTicker.Split(':');
                var googleExchange = tickerParts.Length > 1 ? tickerParts[0] : "";

                // If ticker from col 1 is empty, try from Google ticker
                if (string.IsNullOrEmpty(ticker) && tickerParts.Length > 1)
                {
                    ticker = tickerParts[1];
                }

                var exchange = MapGoogleExchangeToSystem(googleExchange, "");

                // Check for duplicates
                if (existingTickers.Contains(ticker) &&
                    await _context.Instruments.AnyAsync(ins => ins.Symbol.Ticker == ticker && ins.Symbol.Exchange == exchange))
                    continue;

                // Parse name (col 6)
                var name = parts.Length > 6 ? parts[6].Trim() : $"ETF {ticker}";
                if (string.IsNullOrEmpty(name)) name = $"ETF {ticker}";

                // Create instrument
                var symbol = new Domain.ValueObjects.Symbol(ticker, exchange, AssetType.ETF);
                var instrument = new Instrument(symbol, name, isin);

                // Parse ETF details from remaining columns
                int? yearAdded = int.TryParse(parts[0], out var ya) ? ya : null;
                string? region = parts.Length > 4 ? NullIfEmpty(parts[4]) : null;
                string? theme = parts.Length > 5 ? NullIfEmpty(parts[5]) : null;
                string? manager = parts.Length > 7 ? NullIfEmpty(parts[7]) : null;
                string? distributionType = parts.Length > 8 ? NullIfEmpty(parts[8]) : null;
                string? domicile = parts.Length > 10 ? NullIfEmpty(parts[10]) : null;
                string? replication = parts.Length > 11 ? NullIfEmpty(parts[11]) : null;

                // Parse annual fee (col 15), handle "0,65%" format
                decimal? annualFee = null;
                if (parts.Length > 15 && !string.IsNullOrEmpty(parts[15]))
                {
                    var feeStr = parts[15].Trim()
                        .Replace("%", "")
                        .Replace(" ", "")
                        .Replace("\u00A0", "") // Non-breaking space
                        .Replace(",", ".");

                    if (decimal.TryParse(feeStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var fee))
                    {
                        annualFee = fee;
                    }
                }

                // Parse assets (col 16)
                decimal? assets = null;
                if (parts.Length > 16 && !string.IsNullOrEmpty(parts[16]))
                {
                    var assetsStr = parts[16].Trim()
                        .Replace(" ", "")
                        .Replace("\u00A0", "") // Non-breaking space
                        .Replace(",", ".");

                    if (decimal.TryParse(assetsStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var a))
                    {
                        assets = a;
                    }
                }

                string? currency = parts.Length > 18 ? NullIfEmpty(parts[18]) : null;

                var etfDetails = new Domain.Entities.EtfDetails(
                    instrument.Id,
                    yearAdded,
                    region,
                    theme,
                    manager,
                    distributionType,
                    domicile,
                    replication,
                    annualFee,
                    assets,
                    currency,
                    googleTicker
                );

                instruments.Add(instrument);
                etfDetailsList.Add(etfDetails);
                existingTickers.Add(ticker);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing ETF from row {i}: {ex.Message}");
            }
        }

        if (instruments.Count > 0)
        {
            await _context.Instruments.AddRangeAsync(instruments);
            await _context.Set<Domain.Entities.EtfDetails>().AddRangeAsync(etfDetailsList);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Imported {instruments.Count} ETFs with details.");
        }
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string MapGoogleExchangeToSystem(string googlePrefix, string exchangeName)
    {
        return googlePrefix.ToUpper() switch
        {
            "FRA" => "XETRA",
            "LON" => "LSE",
            "AMS" => "Euronext Amsterdam",
            "PAR" => "Euronext Paris",
            "MIL" => "Borsa Italiana",
            "MAD" => "Bolsa de Madrid",
            "HEL" => "Nasdaq Helsinki",
            "STO" => "Nasdaq Stockholm",
            "OSL" => "Oslo Børs",
            "CPH" => "Nasdaq Copenhagen",
            "BRU" => "Euronext Brussels",
            "LIS" => "Euronext Lisbon",
            "DUB" => "Euronext Dublin",
            "VIE" => "Wiener Börse",
            "VTX" => "SIX Swiss Exchange",
            "EBR" => "Euronext Brussels",
            "EPA" => "Euronext Paris",
            "EAM" => "Euronext Amsterdam",
            "BIT" => "Borsa Italiana",
            "WSE" => "GPW",
            _ => !string.IsNullOrEmpty(exchangeName) ? exchangeName : "Europe"
        };
    }

    public async Task ImportGlobalAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File not found: {filePath}");
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var items = JsonSerializer.Deserialize<List<GlobalInstrumentItem>>(json);

        if (items == null) return;

        var instruments = new List<Instrument>();

        // Load existing Tickers AND ISINs to catch all duplicates
        var existingTickers = new HashSet<string>(await _context.Instruments.Select(i => i.Symbol.Ticker).ToListAsync());
        var existingIsins = new HashSet<string>(await _context.Instruments.Select(i => i.Isin).ToListAsync());

        foreach (var item in items)
        {
            var ticker = item.NasdaqSymbol?.Replace("^", "-").Replace("/", "-");
            if (string.IsNullOrWhiteSpace(ticker)) continue;

            // Generate dummy ISIN consistently
            var dummyIsin = $"US{ticker.PadRight(10, 'X')}";
            if (dummyIsin.Length > 20) dummyIsin = dummyIsin[..20];

            // Check if Ticker OR ISIN already exists
            if (existingTickers.Contains(ticker) || existingIsins.Contains(dummyIsin))
                continue;

            try
            {
                var assetType = item.Etf == "Y" ? AssetType.ETF : AssetType.Stock;

                // Map exchange codes
                var exchange = item.Exchange switch
                {
                    "N" => "NYSE",
                    "P" => "NYSE Arca",
                    "A" => "NYSE American",
                    "Z" => "Cboe BZX",
                    _ => "US" // Fallback
                };

                var symbol = new Domain.ValueObjects.Symbol(ticker, exchange, assetType);
                var instrument = new Instrument(symbol, item.SecurityName, dummyIsin);

                instruments.Add(instrument);
                existingTickers.Add(ticker);
                existingIsins.Add(dummyIsin);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating instrument object {item.SecurityName}: {ex.Message}");
            }
        }

        if (instruments.Count > 0)
        {
            // Try to save all at once
            try
            {
                await _context.Instruments.AddRangeAsync(instruments);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Imported {instruments.Count} Global instruments.");
            }
            catch (Exception ex) // Catch generic exception to handle duplicates AND other DbUpdateErrors
            {
                Console.WriteLine($"Bulk import failed: {ex.Message}. Retrying individually...");
                _context.ChangeTracker.Clear();

                // Fallback: Save one by one
                var importedCount = 0;
                foreach (var instrument in instruments)
                {
                    try
                    {
                        // Check logic: Ticker OR ISIN match
                        var exists = await _context.Instruments.AnyAsync(i => i.Symbol.Ticker == instrument.Symbol.Ticker || i.Isin == instrument.Isin);

                        if (!exists)
                        {
                            _context.Instruments.Add(instrument);
                            await _context.SaveChangesAsync();
                            importedCount++;
                        }
                        else
                        {
                            // Optional: UpSert logic if needed, but for now just skip
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine($"Failed to import {instrument.Symbol.Ticker}: {innerEx.Message}");
                        _context.ChangeTracker.Clear();
                    }
                }
                Console.WriteLine($"Recovered {importedCount} instruments individually.");
            }
        }
    }
    private sealed class RootObject
    {
        [JsonPropertyName("r_")]
        public List<InstrumentItem> Rows { get; set; } = [];
    }

    private sealed class InstrumentItem
    {
        [JsonPropertyName("nazwa")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("nazwa_sk")]
        public string ShortName { get; set; } = string.Empty;

        [JsonPropertyName("grupa")]
        public string Group { get; set; } = string.Empty;

        [JsonPropertyName("isin")]
        public string Isin { get; set; } = string.Empty;
    }

    private sealed class GlobalInstrumentItem
    {
        [JsonPropertyName("NASDAQ Symbol")]
        public string NasdaqSymbol { get; set; } = string.Empty;

        [JsonPropertyName("Security Name")]
        public string SecurityName { get; set; } = string.Empty;

        [JsonPropertyName("ETF")]
        public string Etf { get; set; } = string.Empty;

        [JsonPropertyName("Exchange")]
        public string Exchange { get; set; } = string.Empty;
    }
} // End of InstrumentImporter class

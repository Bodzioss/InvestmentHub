using System.Text.Json;
using System.Text.Json.Serialization;
using InvestmentHub.Domain.Entities;
using InvestmentHub.Domain.Enums;
using InvestmentHub.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace InvestmentHub.Infrastructure.Data;

public class InstrumentImporter
{
    private readonly ApplicationDbContext _context;

    public InstrumentImporter(ApplicationDbContext context)
    {
        _context = context;
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

                var symbol = new Symbol(item.ShortName, exchange, assetType);
                var instrument = new Instrument(symbol, item.Name, item.Isin);
                
                instruments.Add(instrument);
                existingIsinSet.Add(item.Isin);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing item {item.Name} ({item.Isin}): {ex.Message}");
            }
        }

        if (instruments.Any())
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

    private sealed class RootObject
    {
        [JsonPropertyName("r_")]
        public List<InstrumentItem> Rows { get; set; } = new();
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
}

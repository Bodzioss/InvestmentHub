using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InvestmentHub.Infrastructure.AI;

/// <summary>
/// Implementation of Gemini AI service using Google's free API.
/// </summary>
public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(IConfiguration config, ILogger<GeminiService> logger)
    {
        _apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API key not configured");
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                model = "models/text-embedding-004",
                content = new { parts = new[] { new { text = text } } }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"v1beta/models/text-embedding-004:embedContent?key={_apiKey}",
                request,
                ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
            return result?.Embedding?.Values ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get embedding from Gemini");
            throw;
        }
    }

    public async Task<string> GenerateResponseAsync(string prompt, string context, CancellationToken ct = default)
    {
        const int maxRetries = 3;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var systemPrompt = $"""
                    You are a financial analyst assistant helping users understand financial reports and data.
                    Answer questions based ONLY on the provided context from uploaded financial documents.
                    If the answer is not in the context, say "I don't have that information in the uploaded documents."
                    Be concise, professional, and cite specific numbers when available.
                    
                    Context from financial documents:
                    {context}
                    """;

                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = $"{systemPrompt}\n\nUser question: {prompt}" }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.3,
                        maxOutputTokens = 2048,
                        topP = 0.95
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}",
                    request,
                    ct);

                // Handle rate limiting with retry
                if ((int)response.StatusCode == 429)
                {
                    var waitTime = (attempt + 1) * 15000; // 15s, 30s, 45s
                    _logger.LogWarning("Rate limited by Gemini API, waiting {WaitTime}ms before retry {Attempt}/{MaxRetries}",
                        waitTime, attempt + 1, maxRetries);
                    await Task.Delay(waitTime, ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(ct);
                var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrEmpty(text))
                {
                    _logger.LogWarning("Empty response from Gemini");
                    return "I couldn't generate a response. Please try again.";
                }

                return text;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                var waitTime = (int)Math.Pow(2, attempt + 1) * 1000;
                _logger.LogWarning("Rate limited (exception), waiting {WaitTime}ms before retry", waitTime);
                await Task.Delay(waitTime, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate response from Gemini");
                return $"Error: {ex.Message}";
            }
        }

        return "Rate limit exceeded. Please wait a moment and try again.";
    }
}

#region Response DTOs

internal class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public EmbeddingData? Embedding { get; set; }
}

internal class EmbeddingData
{
    [JsonPropertyName("values")]
    public float[]? Values { get; set; }
}

internal class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }
}

internal class Candidate
{
    [JsonPropertyName("content")]
    public ContentData? Content { get; set; }
}

internal class ContentData
{
    [JsonPropertyName("parts")]
    public List<PartData>? Parts { get; set; }
}

internal class PartData
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

#endregion

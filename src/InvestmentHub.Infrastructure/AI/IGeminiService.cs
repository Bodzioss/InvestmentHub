namespace InvestmentHub.Infrastructure.AI;

/// <summary>
/// Interface for Google Gemini AI service.
/// </summary>
public interface IGeminiService
{
    /// <summary>
    /// Generates vector embedding for text using Gemini text-embedding-004 model.
    /// </summary>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Generates a response using Gemini 1.5 Flash model with RAG context.
    /// </summary>
    Task<string> GenerateResponseAsync(string prompt, string context, CancellationToken ct = default);
}

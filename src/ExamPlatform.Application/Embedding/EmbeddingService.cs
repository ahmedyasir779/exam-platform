using ExamPlatform.Domain.Interfaces;

namespace ExamPlatform.Application.Embedding;

/// <summary>
/// MVP embedding service using simple keyword-based search.
/// No external API needed — works entirely locally.
/// Replace with real embeddings (OpenAI, etc.) post-MVP.
/// </summary>
public class EmbeddingService(IVectorStore vectorStore)
{
    public Task EmbedAndIndexAsync(string documentId, IReadOnlyList<Domain.Entities.Chunk> chunks, CancellationToken ct = default)
    {
        // Chunks are already stored in DB with their text — no external embedding needed.
        // The LocalVectorStore will use keyword search at query time.
        Console.WriteLine($"[{documentId}] Skipping external embedding — using keyword search.");
        return Task.CompletedTask;
    }

    public Task<float[]> EmbedQueryAsync(string text, CancellationToken ct = default)
    {
        // Return empty vector — LocalVectorStore handles keyword fallback
        return Task.FromResult(Array.Empty<float>());
    }
}

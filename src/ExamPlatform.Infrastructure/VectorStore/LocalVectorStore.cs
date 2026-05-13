using ExamPlatform.Domain.Interfaces;
using ExamPlatform.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace ExamPlatform.Infrastructure.VectorStore;

/// <summary>
/// MVP vector store using keyword-based search against DB chunks.
/// No external embedding API required.
/// </summary>
public class LocalVectorStore(string basePath) : IVectorStore
{
    private readonly record struct VectorEntry(string ChunkId, float[] Vector);

    public Task AddAsync(string documentId, string chunkId, float[] vector, CancellationToken ct = default)
        => Task.CompletedTask; // No-op for keyword search mode

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string documentId, float[] queryVector, int topK, CancellationToken ct = default)
    {
        // queryVector is empty in keyword mode — return empty, caller uses DB chunks directly
        return Array.Empty<VectorSearchResult>();
    }

    public Task DeleteDocumentAsync(string documentId, CancellationToken ct = default)
        => Task.CompletedTask;
}

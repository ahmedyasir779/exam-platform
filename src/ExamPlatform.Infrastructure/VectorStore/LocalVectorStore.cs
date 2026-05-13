using System.Text.Json;
using ExamPlatform.Domain.Interfaces;

namespace ExamPlatform.Infrastructure.VectorStore;

public class LocalVectorStore(string basePath) : IVectorStore
{
    private readonly record struct VectorEntry(string ChunkId, float[] Vector);

    public async Task AddAsync(string documentId, string chunkId, float[] vector, CancellationToken ct = default)
    {
        var entries = await LoadAsync(documentId, ct);
        entries.RemoveAll(e => e.ChunkId == chunkId);
        entries.Add(new VectorEntry(chunkId, vector));
        await SaveAsync(documentId, entries, ct);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string documentId, float[] queryVector, int topK, CancellationToken ct = default)
    {
        var entries = await LoadAsync(documentId, ct);
        return entries
            .Select(e => new VectorSearchResult(e.ChunkId, CosineSimilarity(queryVector, e.Vector)))
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();
    }

    public Task DeleteDocumentAsync(string documentId, CancellationToken ct = default)
    {
        var path = IndexPath(documentId);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string IndexPath(string documentId) => Path.Combine(basePath, $"{documentId}.json");

    private async Task<List<VectorEntry>> LoadAsync(string documentId, CancellationToken ct)
    {
        Directory.CreateDirectory(basePath);
        var path = IndexPath(documentId);
        if (!File.Exists(path)) return [];
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<List<VectorEntry>>(json) ?? [];
    }

    private async Task SaveAsync(string documentId, List<VectorEntry> entries, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(entries);
        await File.WriteAllTextAsync(IndexPath(documentId), json, ct);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        if (magA == 0 || magB == 0) return 0;
        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }
}

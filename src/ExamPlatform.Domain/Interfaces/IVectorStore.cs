namespace ExamPlatform.Domain.Interfaces;

public interface IVectorStore
{
    Task AddAsync(string documentId, string chunkId, float[] vector, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string documentId, float[] queryVector, int topK, CancellationToken ct = default);
    Task DeleteDocumentAsync(string documentId, CancellationToken ct = default);
}

public record VectorSearchResult(string ChunkId, float Score);

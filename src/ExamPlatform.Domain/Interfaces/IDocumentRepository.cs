using ExamPlatform.Domain.Entities;

namespace ExamPlatform.Domain.Interfaces;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    Task UpdateAsync(Document document, CancellationToken ct = default);
    Task AddChunksAsync(IEnumerable<Chunk> chunks, CancellationToken ct = default);
    Task<IReadOnlyList<Chunk>> GetChunksByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}

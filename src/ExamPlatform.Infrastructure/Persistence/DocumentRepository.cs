using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Infrastructure.Persistence;

public class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Documents.Include(d => d.Chunks).FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct = default)
        => await db.Documents.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        db.Documents.Update(document);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await db.Documents.FindAsync([id], ct);
        if (doc is not null) db.Documents.Remove(doc);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddChunksAsync(IEnumerable<Chunk> chunks, CancellationToken ct = default)
    {
        db.Chunks.AddRange(chunks);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Chunk>> GetChunksByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        => await db.Chunks.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
}

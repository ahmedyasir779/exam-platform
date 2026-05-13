using ExamPlatform.Application.DTOs;
using ExamPlatform.Application.PdfProcessing;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Interfaces;

namespace ExamPlatform.Api.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        group.MapPost("/upload", async (
            IFormFile file,
            IFileStorage fileStorage,
            IDocumentRepository documentRepository,
            IServiceProvider serviceProvider,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file provided");

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("Only PDF files are accepted");

            await using var stream = file.OpenReadStream();
            var filePath = await fileStorage.SaveAsync(stream, file.FileName, ct);

            var document = new Document { FilePath = filePath, OriginalName = file.FileName };
            await documentRepository.AddAsync(document, ct);

            var docId = document.Id;

            _ = Task.Run(async () =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var pdfService = scope.ServiceProvider.GetRequiredService<PdfProcessingService>();

                try
                {
                    Console.WriteLine($"[{docId}] Starting PDF processing...");
                    await pdfService.ProcessAsync(docId);
                    Console.WriteLine($"[{docId}] PDF processing complete.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{docId}] Processing failed: {ex.Message}");
                }
            });

            return Results.Ok(new DocumentStatusDto(
                document.Id, document.OriginalName,
                document.ProcessedStatus, null, document.CreatedAt));
        }).DisableAntiforgery();

        group.MapGet("/{id:guid}/status", async (
            Guid id, IDocumentRepository repo, CancellationToken ct) =>
        {
            var doc = await repo.GetByIdAsync(id, ct);
            return doc is null ? Results.NotFound()
                : Results.Ok(new DocumentStatusDto(
                    doc.Id, doc.OriginalName,
                    doc.ProcessedStatus, doc.PageCount, doc.CreatedAt));
        });

        group.MapGet("/", async (IDocumentRepository repo, CancellationToken ct) =>
        {
            var docs = await repo.GetAllAsync(ct);
            return Results.Ok(docs.Select(d =>
                new DocumentListItemDto(d.Id, d.OriginalName, d.ProcessedStatus, d.CreatedAt)));
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IDocumentRepository repo,
            IFileStorage fileStorage,
            IVectorStore vectorStore,
            CancellationToken ct) =>
        {
            var doc = await repo.GetByIdAsync(id, ct);
            if (doc is null) return Results.NotFound();

            // Delete physical file
            try { await fileStorage.DeleteAsync(doc.FilePath, ct); } catch { }

            // Delete vector index
            try { await vectorStore.DeleteDocumentAsync(id.ToString(), ct); } catch { }

            // Delete from DB (cascades to chunks)
            await repo.DeleteAsync(id, ct);

            return Results.Ok();
        });
    }
}

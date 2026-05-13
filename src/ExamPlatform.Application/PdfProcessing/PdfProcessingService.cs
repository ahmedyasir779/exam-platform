using ExamPlatform.Domain.Interfaces;
using UglyToad.PdfPig;

namespace ExamPlatform.Application.PdfProcessing;

public class PdfProcessingService(
    IDocumentRepository documentRepository,
    IFileStorage fileStorage,
    ChunkingStrategy chunkingStrategy)
{
    public async Task ProcessAsync(Guid documentId, CancellationToken ct = default)
    {
        var document = await documentRepository.GetByIdAsync(documentId, ct)
            ?? throw new InvalidOperationException($"Document {documentId} not found");

        document.ProcessedStatus = "processing";
        await documentRepository.UpdateAsync(document, ct);

        try
        {
            var fileStream = await fileStorage.ReadAsync(document.FilePath, ct);
            var rawPages = ExtractPages(fileStream);

            document.PageCount = rawPages.Count;

            var chunks = chunkingStrategy.Chunk(documentId, rawPages);
            await documentRepository.AddChunksAsync(chunks, ct);

            document.ProcessedStatus = "ready";
        }
        catch
        {
            document.ProcessedStatus = "failed";
            throw;
        }
        finally
        {
            await documentRepository.UpdateAsync(document, ct);
        }
    }

    private static IReadOnlyList<RawPage> ExtractPages(Stream stream)
    {
        var pages = new List<RawPage>();

        using var pdf = PdfDocument.Open(stream);
        foreach (var page in pdf.GetPages())
        {
            var words = page.GetWords().ToList();
            var text = string.Join(" ", words.Select(w => w.Text));

            var firstWord = words.FirstOrDefault();
            float? bboxX = null, bboxY = null, bboxWidth = null, bboxHeight = null;

            if (firstWord is not null)
            {
                var rect = firstWord.BoundingBox;
                bboxX = (float)rect.Left;
                bboxY = (float)rect.Bottom;
                bboxWidth = (float)rect.Width;
                bboxHeight = (float)rect.Height;
            }

            pages.Add(new RawPage(page.Number, text, bboxX, bboxY, bboxWidth, bboxHeight));
        }

        return pages;
    }
}

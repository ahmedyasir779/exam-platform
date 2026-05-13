using ExamPlatform.Domain.Entities;

namespace ExamPlatform.Application.PdfProcessing;

public record RawPage(int PageNumber, string Text, float? BboxX, float? BboxY, float? BboxWidth, float? BboxHeight);

public class ChunkingStrategy
{
    private const int MaxTokens = 512;
    private const int OverlapTokens = 64;

    private static string[] Tokenize(string text) =>
        text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    public IReadOnlyList<Chunk> Chunk(Guid documentId, IEnumerable<RawPage> pages)
    {
        var chunks = new List<Chunk>();

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text)) continue;

            var tokens = Tokenize(page.Text);

            for (int start = 0; start < tokens.Length; start += MaxTokens - OverlapTokens)
            {
                var slice = tokens.Skip(start).Take(MaxTokens).ToArray();
                if (slice.Length == 0) break;

                chunks.Add(new Chunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    Page = page.PageNumber,
                    Text = string.Join(" ", slice),
                    BboxX = page.BboxX,
                    BboxY = page.BboxY,
                    BboxWidth = page.BboxWidth,
                    BboxHeight = page.BboxHeight
                });
            }
        }

        return chunks;
    }
}

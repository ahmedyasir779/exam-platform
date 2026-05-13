using ExamPlatform.Domain.Entities;
using System.Text.RegularExpressions;

namespace ExamPlatform.Application.PdfProcessing;

public record RawPage(int PageNumber, string Text, float? BboxX, float? BboxY, float? BboxWidth, float? BboxHeight);

public class ChunkingStrategy
{
    private const int MaxTokens = 400;
    private const int OverlapTokens = 50;

    // Detect Arabic by checking if text contains Arabic Unicode block characters
    public static bool IsArabic(string text)
    {
        var arabicCount = text.Count(c => c >= 0x0600 && c <= 0x06FF);
        return arabicCount > text.Length * 0.2; // more than 20% Arabic chars
    }

    private static string[] TokenizeArabic(string text)
    {
        // Split on sentence boundaries for Arabic (period, Arabic full stop, newlines)
        // Arabic full stop is U+06D4, also split on \n and Western period
        var sentences = Regex.Split(text, @"(?<=[.?!?\n\u06D4])");
        return sentences
            .Select(s => s.Trim())
            .Where(s => s.Length > 10)
            .ToArray();
    }

    private static string[] TokenizeStandard(string text) =>
        text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    public IReadOnlyList<Chunk> Chunk(Guid documentId, IEnumerable<RawPage> pages)
    {
        var chunks = new List<Chunk>();

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text)) continue;

            var arabic = IsArabic(page.Text);

            if (arabic)
                ChunkArabic(documentId, page, chunks);
            else
                ChunkStandard(documentId, page, chunks);
        }

        return chunks;
    }

    private static void ChunkStandard(Guid documentId, RawPage page, List<Chunk> chunks)
    {
        var tokens = TokenizeStandard(page.Text);

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

    private static void ChunkArabic(Guid documentId, RawPage page, List<Chunk> chunks)
    {
        var sentences = TokenizeArabic(page.Text);
        if (sentences.Length == 0)
        {
            // Fallback: chunk by character count for Arabic
            var text = page.Text.Trim();
            int chunkSize = 800; // characters
            int overlap = 100;

            for (int start = 0; start < text.Length; start += chunkSize - overlap)
            {
                var slice = text.Substring(start, Math.Min(chunkSize, text.Length - start));
                if (slice.Trim().Length < 20) break;

                chunks.Add(new Chunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    Page = page.PageNumber,
                    Text = slice.Trim(),
                    BboxX = page.BboxX,
                    BboxY = page.BboxY,
                    BboxWidth = page.BboxWidth,
                    BboxHeight = page.BboxHeight
                });
            }
            return;
        }

        // Group sentences into chunks of ~MaxTokens words
        var current = new List<string>();
        int currentLen = 0;

        foreach (var sentence in sentences)
        {
            var wordCount = sentence.Split(' ').Length;
            if (currentLen + wordCount > MaxTokens && current.Count > 0)
            {
                chunks.Add(new Chunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    Page = page.PageNumber,
                    Text = string.Join(" ", current),
                    BboxX = page.BboxX,
                    BboxY = page.BboxY,
                    BboxWidth = page.BboxWidth,
                    BboxHeight = page.BboxHeight
                });

                // Keep last sentence for overlap
                current = current.TakeLast(2).ToList();
                currentLen = current.Sum(s => s.Split(' ').Length);
            }

            current.Add(sentence);
            currentLen += wordCount;
        }

        if (current.Count > 0)
        {
            chunks.Add(new Chunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Page = page.PageNumber,
                Text = string.Join(" ", current),
                BboxX = page.BboxX,
                BboxY = page.BboxY,
                BboxWidth = page.BboxWidth,
                BboxHeight = page.BboxHeight
            });
        }
    }
}

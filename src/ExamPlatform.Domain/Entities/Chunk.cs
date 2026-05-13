namespace ExamPlatform.Domain.Entities;

public class Chunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public int Page { get; set; }
    public string Text { get; set; } = string.Empty;
    public float? BboxX { get; set; }
    public float? BboxY { get; set; }
    public float? BboxWidth { get; set; }
    public float? BboxHeight { get; set; }
    public string? EmbeddingId { get; set; }

    public Document Document { get; set; } = null!;
}

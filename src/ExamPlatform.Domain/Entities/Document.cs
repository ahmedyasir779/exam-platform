namespace ExamPlatform.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilePath { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string ProcessedStatus { get; set; } = "pending";
    public int? PageCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Chunk> Chunks { get; set; } = new List<Chunk>();
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}

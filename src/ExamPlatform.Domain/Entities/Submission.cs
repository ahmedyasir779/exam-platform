namespace ExamPlatform.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamId { get; set; }
    public string? StudentId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public float? TotalScore { get; set; }

    public Exam Exam { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}

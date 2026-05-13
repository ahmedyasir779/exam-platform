namespace ExamPlatform.Domain.Entities;

public class Exam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid? TemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Document Document { get; set; } = null!;
    public Template? Template { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}

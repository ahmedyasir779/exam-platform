namespace ExamPlatform.Domain.Entities;

public class Answer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public Guid QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public float? Score { get; set; }
    public string? Feedback { get; set; }
    public int? SourcePage { get; set; }

    public Submission Submission { get; set; } = null!;
    public Question Question { get; set; } = null!;
}

using System.Text.Json;

namespace ExamPlatform.Domain.Entities;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public JsonDocument? Options { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public int SourcePage { get; set; }
    public string? SourceSnippet { get; set; }
    public JsonDocument? SourceBbox { get; set; }
    public int Position { get; set; }

    public Exam Exam { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}

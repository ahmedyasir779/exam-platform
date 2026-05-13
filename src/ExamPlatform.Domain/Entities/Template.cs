using System.Text.Json;

namespace ExamPlatform.Domain.Entities;

public class Template
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public JsonDocument StructureJson { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}

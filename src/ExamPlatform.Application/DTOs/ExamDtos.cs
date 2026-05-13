namespace ExamPlatform.Application.DTOs;

public record GenerateExamRequest(
    Guid DocumentId,
    string Title,
    ExamTemplateDto Template,
    int FromPage = 1,
    int ToPage = int.MaxValue
);

public record ExamTemplateDto(List<QuestionTypeCount> QuestionTypes, string Difficulty, string Language);
public record QuestionTypeCount(string Type, int Count);

public record QuestionDto(
    Guid Id, string Type, string QuestionText,
    List<string>? Options, string CorrectAnswer,
    int SourcePage, string? SourceSnippet, BboxDto? SourceBbox, int Position);

public record BboxDto(float X, float Y, float Width, float Height);

public record ExamDto(Guid Id, string Title, Guid DocumentId, DateTime CreatedAt, List<QuestionDto> Questions);

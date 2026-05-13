namespace ExamPlatform.Application.DTOs;

public record DocumentStatusDto(Guid Id, string OriginalName, string Status, int? PageCount, DateTime CreatedAt);
public record DocumentListItemDto(Guid Id, string OriginalName, string Status, DateTime CreatedAt);

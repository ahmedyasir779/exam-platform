namespace ExamPlatform.Application.DTOs;

public record SubmitAnswersRequest(Guid ExamId, string? StudentId, List<AnswerInput> Answers);
public record AnswerInput(Guid QuestionId, string AnswerText);
public record GradingResultDto(Guid SubmissionId, float TotalScore, List<AnswerResultDto> Answers);
public record AnswerResultDto(Guid QuestionId, string StudentAnswer, float Score, string? Feedback, int? SourcePage);

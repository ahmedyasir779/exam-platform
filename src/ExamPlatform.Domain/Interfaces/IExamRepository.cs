using ExamPlatform.Domain.Entities;

namespace ExamPlatform.Domain.Interfaces;

public interface IExamRepository
{
    Task<Exam?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Exam exam, CancellationToken ct = default);
    Task UpdateAsync(Exam exam, CancellationToken ct = default);
    Task<Question?> GetQuestionByIdAsync(Guid questionId, CancellationToken ct = default);
    Task UpdateQuestionAsync(Question question, CancellationToken ct = default);
    Task DeleteQuestionAsync(Guid questionId, CancellationToken ct = default);
    Task AddSubmissionAsync(Submission submission, CancellationToken ct = default);
    Task<Submission?> GetSubmissionByIdAsync(Guid submissionId, CancellationToken ct = default);
}

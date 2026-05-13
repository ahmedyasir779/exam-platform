using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Infrastructure.Persistence;

public class ExamRepository(AppDbContext db) : IExamRepository
{
    public async Task<Exam?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Exams
            .Include(e => e.Questions.OrderBy(q => q.Position))
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(Exam exam, CancellationToken ct = default)
    {
        db.Exams.Add(exam);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Exam exam, CancellationToken ct = default)
    {
        db.Exams.Update(exam);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Question?> GetQuestionByIdAsync(Guid questionId, CancellationToken ct = default)
        => await db.Questions.FindAsync([questionId], ct);

    public async Task UpdateQuestionAsync(Question question, CancellationToken ct = default)
    {
        db.Questions.Update(question);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteQuestionAsync(Guid questionId, CancellationToken ct = default)
    {
        var q = await db.Questions.FindAsync([questionId], ct);
        if (q is not null) db.Questions.Remove(q);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddSubmissionAsync(Submission submission, CancellationToken ct = default)
    {
        db.Submissions.Add(submission);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Submission?> GetSubmissionByIdAsync(Guid submissionId, CancellationToken ct = default)
        => await db.Submissions
            .Include(s => s.Answers).ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);
}

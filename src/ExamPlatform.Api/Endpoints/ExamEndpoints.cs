using ExamPlatform.Application.DTOs;
using ExamPlatform.Application.ExamGeneration;
using ExamPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Api.Endpoints;

public static class ExamEndpoints
{
    public static void MapExamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/exams").WithTags("Exams");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var exams = await db.Exams
                .Include(e => e.Questions)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.DocumentId,
                    e.CreatedAt,
                    QuestionCount = e.Questions.Count
                })
                .ToListAsync(ct);
            return Results.Ok(exams);
        });

        group.MapPost("/generate", async (
            GenerateExamRequest request,
            ExamGenerationService service,
            CancellationToken ct) =>
        {
            var exam = await service.GenerateAsync(request, ct);
            return Results.Ok(exam);
        });

        group.MapGet("/{id:guid}", async (
            Guid id, ExamGenerationService service, CancellationToken ct) =>
        {
            var exam = await service.GetByIdAsync(id, ct);
            return exam is null ? Results.NotFound() : Results.Ok(exam);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var exam = await db.Exams.FindAsync([id], ct);
            if (exam is null) return Results.NotFound();
            db.Exams.Remove(exam);
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        });
    }
}

using ExamPlatform.Application.DTOs;
using ExamPlatform.Application.Grading;
using ExamPlatform.Domain.Interfaces;

namespace ExamPlatform.Api.Endpoints;

public static class SubmissionEndpoints
{
    public static void MapSubmissionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/submissions").WithTags("Submissions");

        group.MapPost("/", async (
            SubmitAnswersRequest request,
            GradingService gradingService,
            CancellationToken ct) =>
        {
            var result = await gradingService.GradeAsync(request, ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}/results", async (
            Guid id, IExamRepository examRepository, CancellationToken ct) =>
        {
            var submission = await examRepository.GetSubmissionByIdAsync(id, ct);
            return submission is null ? Results.NotFound() : Results.Ok(submission);
        });
    }
}

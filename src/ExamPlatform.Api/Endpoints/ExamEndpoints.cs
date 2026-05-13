using ExamPlatform.Application.DTOs;
using ExamPlatform.Application.ExamGeneration;

namespace ExamPlatform.Api.Endpoints;

public static class ExamEndpoints
{
    public static void MapExamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/exams").WithTags("Exams");

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
    }
}

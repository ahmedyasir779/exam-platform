using ExamPlatform.Application.Export;
using ExamPlatform.Application.ExamGeneration;

namespace ExamPlatform.Api.Endpoints;

public static class ExportEndpoints
{
    public static void MapExportEndpoints(this WebApplication app)
    {
        app.MapGet("/api/exams/{id:guid}/export/pdf", async (
            Guid id, ExamGenerationService examService,
            PdfExportService pdfService, CancellationToken ct) =>
        {
            var exam = await examService.GetByIdAsync(id, ct);
            if (exam is null) return Results.NotFound();
            var bytes = pdfService.GeneratePdf(exam);
            return Results.File(bytes, "application/pdf", $"{exam.Title}.pdf");
        });

        app.MapGet("/api/exams/{id:guid}/export/docx", async (
            Guid id, ExamGenerationService examService,
            DocxExportService docxService, CancellationToken ct) =>
        {
            var exam = await examService.GetByIdAsync(id, ct);
            if (exam is null) return Results.NotFound();
            var bytes = docxService.GenerateDocx(exam);
            return Results.File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"{exam.Title}.docx");
        });
    }
}

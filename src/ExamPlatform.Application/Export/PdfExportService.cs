using ExamPlatform.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ExamPlatform.Application.Export;

public class PdfExportService
{
    static PdfExportService() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] GeneratePdf(ExamDto exam)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header().Text(exam.Title).SemiBold().FontSize(20);

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    int num = 1;
                    foreach (var q in exam.Questions)
                    {
                        col.Item().Column(qCol =>
                        {
                            qCol.Item().Text($"Q{num++}. ({q.Type}) {q.QuestionText}").SemiBold();

                            if (q.Options is not null)
                            {
                                char letter = 'A';
                                foreach (var opt in q.Options)
                                    qCol.Item().Text($"   {letter++}. {opt}").FontSize(11);
                            }

                            qCol.Item().Text($"Source: Page {q.SourcePage}")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamPlatform.Application.DTOs;

namespace ExamPlatform.Application.Export;

public class DocxExportService
{
    public byte[] GenerateDocx(ExamDto exam)
    {
        using var ms = new MemoryStream();
        using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true);

        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        body.AppendChild(new Paragraph(new Run(new Text(exam.Title))
        {
            RunProperties = new RunProperties(new Bold(), new FontSize { Val = "48" })
        }));

        int num = 1;
        foreach (var q in exam.Questions)
        {
            body.AppendChild(new Paragraph(new Run(new Text($"Q{num++}. [{q.Type}] {q.QuestionText}"))
            {
                RunProperties = new RunProperties(new Bold())
            }));

            if (q.Options is not null)
            {
                char letter = 'A';
                foreach (var opt in q.Options)
                    body.AppendChild(new Paragraph(new Run(new Text($"   {letter++}. {opt}"))));
            }

            body.AppendChild(new Paragraph(new Run(new Text($"Source: Page {q.SourcePage}"))
            {
                RunProperties = new RunProperties(new Color { Val = "808080" })
            }));

            body.AppendChild(new Paragraph());
        }

        body.AppendChild(new Paragraph(new Run(new Text("--- ANSWER KEY ---"))
        {
            RunProperties = new RunProperties(new Bold(), new FontSize { Val = "32" })
        }));

        num = 1;
        foreach (var q in exam.Questions)
        {
            body.AppendChild(new Paragraph(new Run(new Text($"Q{num++}: {q.CorrectAnswer}"))
            {
                RunProperties = new RunProperties(new Bold())
            }));
        }

        mainPart.Document.Save();
        return ms.ToArray();
    }
}

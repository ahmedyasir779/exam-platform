using System.Text.Json;
using ExamPlatform.Application.DTOs;
using ExamPlatform.Application.Embedding;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Interfaces;

namespace ExamPlatform.Application.ExamGeneration;

public class ExamGenerationService(
    IExamRepository examRepository,
    IDocumentRepository documentRepository,
    EmbeddingService embeddingService,
    IVectorStore vectorStore,
    GrokClient grokClient)
{
    public async Task<ExamDto> GenerateAsync(GenerateExamRequest request, CancellationToken ct = default)
    {
        var exam = new Exam
        {
            DocumentId = request.DocumentId,
            Title = request.Title
        };

        var questions = new List<Question>();
        int position = 0;

        foreach (var slot in request.Template.QuestionTypes)
        {
            for (int i = 0; i < slot.Count; i++)
            {
                var queryVector = await embeddingService.EmbedQueryAsync(
                    $"{slot.Type} question about the document content", ct);

                var results = await vectorStore.SearchAsync(
                    request.DocumentId.ToString(), queryVector, topK: 5, ct);

                var chunkIds = results.Select(r => Guid.Parse(r.ChunkId)).ToList();
                var chunks = await documentRepository.GetChunksByIdsAsync(chunkIds, ct);
                var contextTexts = chunks.Select(c => $"[Page {c.Page}] {c.Text}").ToList();

                var generated = await grokClient.GenerateQuestionAsync(
                    slot.Type, request.Template.Difficulty,
                    request.Template.Language, contextTexts, ct);

                var sourceChunk = chunks.FirstOrDefault(c => c.Page == generated.SourcePage)
                    ?? chunks.FirstOrDefault();

                questions.Add(new Question
                {
                    ExamId = exam.Id,
                    Type = slot.Type,
                    QuestionText = generated.QuestionText,
                    Options = generated.Options is not null
                        ? JsonDocument.Parse(JsonSerializer.Serialize(generated.Options))
                        : null,
                    CorrectAnswer = generated.CorrectAnswer,
                    SourcePage = generated.SourcePage,
                    SourceSnippet = generated.SourceSnippet,
                    SourceBbox = sourceChunk is not null
                        ? JsonDocument.Parse(JsonSerializer.Serialize(new
                        {
                            x = sourceChunk.BboxX,
                            y = sourceChunk.BboxY,
                            width = sourceChunk.BboxWidth,
                            height = sourceChunk.BboxHeight
                        }))
                        : null,
                    Position = position++
                });
            }
        }

        exam.Questions = questions;
        await examRepository.AddAsync(exam, ct);
        return ToDto(exam);
    }

    public async Task<ExamDto?> GetByIdAsync(Guid examId, CancellationToken ct = default)
    {
        var exam = await examRepository.GetByIdAsync(examId, ct);
        return exam is null ? null : ToDto(exam);
    }

    public static ExamDto ToDto(Exam exam) => new(
        exam.Id,
        exam.Title,
        exam.DocumentId,
        exam.CreatedAt,
        exam.Questions.OrderBy(q => q.Position).Select(q => new QuestionDto(
            q.Id, q.Type, q.QuestionText,
            q.Options is not null
                ? JsonSerializer.Deserialize<List<string>>(q.Options.RootElement.GetRawText())
                : null,
            q.CorrectAnswer,
            q.SourcePage, q.SourceSnippet,
            q.SourceBbox is not null ? new BboxDto(
                q.SourceBbox.RootElement.GetProperty("x").GetSingle(),
                q.SourceBbox.RootElement.GetProperty("y").GetSingle(),
                q.SourceBbox.RootElement.GetProperty("width").GetSingle(),
                q.SourceBbox.RootElement.GetProperty("height").GetSingle()) : null,
            q.Position
        )).ToList()
    );
}

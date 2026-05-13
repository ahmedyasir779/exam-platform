using ExamPlatform.Application.DTOs;
using ExamPlatform.Application.Embedding;
using ExamPlatform.Application.ExamGeneration;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Interfaces;

namespace ExamPlatform.Application.Grading;

public class GradingService(
    IExamRepository examRepository,
    IDocumentRepository documentRepository,
    EmbeddingService embeddingService,
    IVectorStore vectorStore,
    GrokClient grokClient)
{
    public async Task<GradingResultDto> GradeAsync(SubmitAnswersRequest request, CancellationToken ct = default)
    {
        var exam = await examRepository.GetByIdAsync(request.ExamId, ct)
            ?? throw new InvalidOperationException($"Exam {request.ExamId} not found");

        var submission = new Submission
        {
            ExamId = exam.Id,
            StudentId = request.StudentId
        };

        var answerResults = new List<AnswerResultDto>();
        float totalScore = 0;

        foreach (var input in request.Answers)
        {
            var question = exam.Questions.FirstOrDefault(q => q.Id == input.QuestionId);
            if (question is null) continue;

            float score;
            string? feedback;
            int? sourcePage = null;

            if (question.Type is "mcq" or "true_false")
            {
                score = string.Equals(
                    input.AnswerText.Trim(),
                    question.CorrectAnswer.Trim(),
                    StringComparison.OrdinalIgnoreCase) ? 10f : 0f;

                feedback = score > 0
                    ? "Correct."
                    : $"Incorrect. The correct answer is: {question.CorrectAnswer}";
            }
            else
            {
                var queryVector = await embeddingService.EmbedQueryAsync(question.QuestionText, ct);
                var results = await vectorStore.SearchAsync(
                    exam.DocumentId.ToString(), queryVector, topK: 3, ct);

                var chunkIds = results.Select(r => Guid.Parse(r.ChunkId)).ToList();
                var chunks = await documentRepository.GetChunksByIdsAsync(chunkIds, ct);
                var contextTexts = chunks.Select(c => $"[Page {c.Page}] {c.Text}").ToList();

                var grading = await grokClient.GradeAnswerAsync(
                    question.QuestionText, input.AnswerText, contextTexts, ct);

                score = grading.Score;
                feedback = grading.Feedback;
                sourcePage = grading.SourcePage;
            }

            totalScore += score;
            submission.Answers.Add(new Answer
            {
                SubmissionId = submission.Id,
                QuestionId = question.Id,
                AnswerText = input.AnswerText,
                Score = score,
                Feedback = feedback,
                SourcePage = sourcePage
            });

            answerResults.Add(new AnswerResultDto(
                question.Id, input.AnswerText, score, feedback, sourcePage));
        }

        submission.TotalScore = request.Answers.Count > 0
            ? totalScore / request.Answers.Count
            : 0;

        await examRepository.AddSubmissionAsync(submission, ct);

        return new GradingResultDto(submission.Id, submission.TotalScore.Value, answerResults);
    }
}

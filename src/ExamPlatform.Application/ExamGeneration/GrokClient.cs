using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExamPlatform.Application.ExamGeneration;

public class GrokClient(HttpClient httpClient)
{
    private const string BaseUrl = "https://api.x.ai/v1/chat/completions";
    private const string Model = "grok-3";

    public async Task<GeneratedQuestion> GenerateQuestionAsync(
        string questionType,
        string difficulty,
        string language,
        IEnumerable<string> contextChunks,
        CancellationToken ct = default)
    {
        var context = string.Join("\n\n---\n\n", contextChunks);

        var systemPrompt =
            "You are an expert exam question generator. " +
            "Use ONLY the provided context to generate questions. " +
            "Never invent facts not present in the context. " +
            "Return valid JSON only. No markdown, no explanation, no code fences.";

        var userPrompt =
            $"Context:\n{context}\n\n" +
            $"Generate ONE {questionType} question at {difficulty} difficulty in {language} language.\n" +
            $"Return this exact JSON shape:\n" +
            $"{{\"question_text\":\"...\",\"options\":null,\"correct_answer\":\"...\",\"source_page\":1,\"source_snippet\":\"...\"}}";

        var payload = new
        {
            model = Model,
            temperature = 0,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var response = await httpClient.PostAsJsonAsync(BaseUrl, payload, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var content = json
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        content = CleanJson(content);

        return JsonSerializer.Deserialize<GeneratedQuestion>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Grok returned invalid JSON for question generation");
    }

    public async Task<GradingResult> GradeAnswerAsync(
        string questionText,
        string studentAnswer,
        IEnumerable<string> referenceChunks,
        CancellationToken ct = default)
    {
        var reference = string.Join("\n\n---\n\n", referenceChunks);

        var systemPrompt =
            "You are a strict academic grader. " +
            "Score the student answer ONLY against the provided reference material. " +
            "Do not award marks for content not in the reference. " +
            "Return valid JSON only. No markdown, no explanation, no code fences.\n" +
            "JSON shape: {\"score\":0,\"feedback\":\"...\",\"source_page\":1}";

        var userPrompt =
            $"Question: {questionText}\n\n" +
            $"Reference material:\n{reference}\n\n" +
            $"Student answer: {studentAnswer}\n\n" +
            $"Score from 0 to 10. Explain what was correct and what was missing.";

        var payload = new
        {
            model = Model,
            temperature = 0,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var response = await httpClient.PostAsJsonAsync(BaseUrl, payload, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var content = json
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        content = CleanJson(content);

        return JsonSerializer.Deserialize<GradingResult>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new GradingResult(0, "Could not parse grading response", 0);
    }

    private static string CleanJson(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            if (firstNewline >= 0) content = content[(firstNewline + 1)..];
            var lastFence = content.LastIndexOf("```");
            if (lastFence >= 0) content = content[..lastFence];
        }
        return content.Trim();
    }
}

public record GeneratedQuestion(
    [property: JsonPropertyName("question_text")] string QuestionText,
    [property: JsonPropertyName("options")] List<string>? Options,
    [property: JsonPropertyName("correct_answer")] string CorrectAnswer,
    [property: JsonPropertyName("source_page")] int SourcePage,
    [property: JsonPropertyName("source_snippet")] string? SourceSnippet
);

public record GradingResult(
    [property: JsonPropertyName("score")] float Score,
    [property: JsonPropertyName("feedback")] string Feedback,
    [property: JsonPropertyName("source_page")] int SourcePage
);

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExamPlatform.Application.PdfProcessing;

namespace ExamPlatform.Application.ExamGeneration;

public class GrokClient(HttpClient httpClient)
{
    private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string Model = "llama-3.3-70b-versatile";

    public async Task<GeneratedQuestion> GenerateQuestionAsync(
        string questionType, string difficulty, string language,
        IEnumerable<string> contextChunks, CancellationToken ct = default)
    {
        var chunkList = contextChunks.ToList();
        var context = string.Join("\n\n---\n\n", chunkList);
        var isArabic = ChunkingStrategy.IsArabic(context);

        string systemPrompt, userPrompt;

        if (isArabic)
        {
            var arabicType = TranslateQuestionTypeToArabic(questionType);
            var arabicDifficulty = TranslateDifficultyToArabic(difficulty);

            systemPrompt =
                "??? ???? ?? ????? ????? ?????????? ?????????. " +
                "?????? ??? ????????? ??????? ?? ???? ??????. " +
                "?? ????? ??????? ??? ?????? ?? ????. " +
                "??? ????? JSON ???? ???? ?? ??? ?? ?????? markdown. " +
                "correct_answer ??? ?? ???? ???? ??????. ??????? ????/??? ?????? \"????\" ?? \"???\". " +
                "source_page ??? ?? ???? ????? ??????. " +
                "source_snippet ??? ?? ???? ???? ????? ??? ??? ????.";

            userPrompt =
                $"????:\n{context}\n\n" +
                $"???? ?????? ?????? ?? ??? ({arabicType}) ?????? ????? ({arabicDifficulty}).\n" +
                "??? ??? JSON ???:\n" +
                "{\"question_text\":\"...\",\"options\":null,\"correct_answer\":\"...\",\"source_page\":1,\"source_snippet\":\"...\"}";
        }
        else
        {
            systemPrompt =
                "You are an expert exam question generator. Use ONLY the provided context. " +
                "Return valid JSON only. No markdown, no code fences. " +
                "correct_answer must be a string. For true/false use \"True\" or \"False\". " +
                "source_page must be an integer. source_snippet must be a short single-line string.";

            userPrompt =
                $"Context:\n{context}\n\n" +
                $"Generate ONE {questionType} question at {difficulty} difficulty in {language} language.\n" +
                "Return ONLY this JSON:\n" +
                "{\"question_text\":\"...\",\"options\":null,\"correct_answer\":\"...\",\"source_page\":1,\"source_snippet\":\"...\"}";
        }

        var raw = await CallGroqAsync(systemPrompt, userPrompt, ct);
        return ParseQuestion(raw);
    }

    public async Task<GradingResult> GradeAnswerAsync(
        string questionText, string studentAnswer,
        IEnumerable<string> referenceChunks, CancellationToken ct = default)
    {
        var chunkList = referenceChunks.ToList();
        var reference = string.Join("\n\n---\n\n", chunkList);
        var isArabic = ChunkingStrategy.IsArabic(reference) || ChunkingStrategy.IsArabic(questionText);

        string systemPrompt, userPrompt;

        if (isArabic)
        {
            systemPrompt =
                "??? ???? ??????? ????. ???? ????? ?????? ??? ????? ??? ?????? ???????? ???????. " +
                "?? ???? ????? ??? ????? ??? ????? ?? ??????. " +
                "??? ????? JSON ???: {\"score\":5,\"feedback\":\"...\",\"source_page\":1}";

            userPrompt =
                $"??????: {questionText}\n\n" +
                $"?????? ????????:\n{reference}\n\n" +
                $"????? ??????: {studentAnswer}\n\n" +
                "???? ??????? ?? 0 ??? 10. ???? ?? ??? ?????? ??? ??? ??????.";
        }
        else
        {
            systemPrompt =
                "You are a strict academic grader. Score ONLY against the reference. " +
                "Return valid JSON only. No markdown, no code fences.\n" +
                "JSON: {\"score\":5,\"feedback\":\"...\",\"source_page\":1}";

            userPrompt =
                $"Question: {questionText}\n\nReference:\n{reference}\n\nStudent answer: {studentAnswer}\n\nScore 0-10.";
        }

        var raw = await CallGroqAsync(systemPrompt, userPrompt, ct);
        return ParseGrading(raw);
    }

    private static string TranslateQuestionTypeToArabic(string type) => type switch
    {
        "mcq" => "?????? ?? ?????",
        "true_false" => "?? ?? ???",
        "short_answer" => "????? ?????",
        "definition" => "?????",
        _ => type
    };

    private static string TranslateDifficultyToArabic(string difficulty) => difficulty switch
    {
        "easy" => "???",
        "medium" => "?????",
        "hard" => "???",
        _ => difficulty
    };

    private async Task<string> CallGroqAsync(string system, string user, CancellationToken ct)
    {
        var payload = new
        {
            model = Model,
            temperature = 0,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
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

        return CleanJson(content);
    }

    private static GeneratedQuestion ParseQuestion(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var questionText = root.TryGetProperty("question_text", out var qt) ? qt.GetString() ?? "" : "";
            var correctAnswer = GetStringOrBool(root, "correct_answer");
            var sourcePage = GetIntOrString(root, "source_page");
            var sourceSnippet = root.TryGetProperty("source_snippet", out var ss) ? ss.GetString() : null;

            List<string>? options = null;
            if (root.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                options = opts.EnumerateArray().Select(o => o.GetString() ?? "").ToList();

            return new GeneratedQuestion(questionText, options, correctAnswer, sourcePage, sourceSnippet);
        }
        catch
        {
            return new GeneratedQuestion("Could not parse question from AI response.", null, "N/A", 1, null);
        }
    }

    private static GradingResult ParseGrading(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var score = (float)GetIntOrString(root, "score");
            var feedback = root.TryGetProperty("feedback", out var f) ? f.GetString() ?? "" : "";
            var sourcePage = GetIntOrString(root, "source_page");
            return new GradingResult(score, feedback, sourcePage);
        }
        catch
        {
            return new GradingResult(0, "Could not parse grading response.", 1);
        }
    }

    private static string GetStringOrBool(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var val)) return "";
        return val.ValueKind switch
        {
            JsonValueKind.True => "True",
            JsonValueKind.False => "False",
            JsonValueKind.String => val.GetString() ?? "",
            _ => val.ToString()
        };
    }

    private static int GetIntOrString(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var val)) return 1;
        if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var n)) return n;
        if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var ns)) return ns;
        return 1;
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

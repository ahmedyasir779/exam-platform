using System.Net.Http.Json;
using System.Text.Json;
using ExamPlatform.Domain.Interfaces;

namespace ExamPlatform.Application.Embedding;

public class EmbeddingService(HttpClient httpClient, IVectorStore vectorStore)
{
    private const string Model = "nomic-embed-text-v1_5";
    private const string BaseUrl = "https://api.groq.com/openai/v1/embeddings";

    public async Task EmbedAndIndexAsync(string documentId, IReadOnlyList<Domain.Entities.Chunk> chunks, CancellationToken ct = default)
    {
        foreach (var batch in chunks.Chunk(50))
        {
            var inputs = batch.Select(c => c.Text).ToList();
            var vectors = await GetEmbeddingsAsync(inputs, ct);

            for (int i = 0; i < batch.Length; i++)
            {
                var chunk = batch[i];
                chunk.EmbeddingId = chunk.Id.ToString();
                await vectorStore.AddAsync(documentId, chunk.Id.ToString(), vectors[i], ct);
            }
        }
    }

    public async Task<float[]> EmbedQueryAsync(string text, CancellationToken ct = default)
    {
        var vectors = await GetEmbeddingsAsync([text], ct);
        return vectors[0];
    }

    private async Task<List<float[]>> GetEmbeddingsAsync(List<string> inputs, CancellationToken ct)
    {
        var request = new { model = Model, input = inputs };
        var response = await httpClient.PostAsJsonAsync(BaseUrl, request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("data")
            .EnumerateArray()
            .OrderBy(d => d.GetProperty("index").GetInt32())
            .Select(d => d.GetProperty("embedding")
                .EnumerateArray()
                .Select(v => v.GetSingle())
                .ToArray())
            .ToList();
    }
}

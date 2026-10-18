using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace DocClassify.Api.Services;

public class LlmOptions
{
    public string BaseUrl { get; set; } = "http://localhost:1234/v1";
    public string ChatModel { get; set; } = "local-model";
    public string EmbedModel { get; set; } = "embedding-model";
}

/// <summary>OpenAI-compatible client for LM Studio / Ollama. Returns null on failure.</summary>
public class LlmService(HttpClient http, IOptions<LlmOptions> opt, ILogger<LlmService> log)
{
    public async Task<string?> ChatAsync(string system, string user)
    {
        try
        {
            var body = new
            {
                model = opt.Value.ChatModel,
                temperature = 0,
                messages = new object[] { new { role = "system", content = system }, new { role = "user", content = user } }
            };
            using var res = await http.PostAsJsonAsync($"{opt.Value.BaseUrl}/chat/completions", body);
            res.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
        catch (Exception e) { log.LogWarning(e, "LLM chat unavailable"); return null; }
    }

    public async Task<float[]?> EmbedAsync(string text)
    {
        try
        {
            using var res = await http.PostAsJsonAsync($"{opt.Value.BaseUrl}/embeddings", new { model = opt.Value.EmbedModel, input = text });
            res.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("data")[0].GetProperty("embedding")
                .EnumerateArray().Select(x => x.GetSingle()).ToArray();
        }
        catch (Exception e) { log.LogWarning(e, "LLM embeddings unavailable"); return null; }
    }
}

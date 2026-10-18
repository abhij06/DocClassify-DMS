using System.Text.RegularExpressions;

namespace DocClassify.Api.Services;

/// <summary>Real embeddings from the local model; falls back to a hashed bag-of-words vector (256-d).</summary>
public class EmbeddingService(LlmService llm)
{
    const int FallbackDim = 256;

    public async Task<float[]> EmbedAsync(string text) =>
        await llm.EmbedAsync(text) ?? HashEmbed(text);

    public static float[] HashEmbed(string text)
    {
        var v = new float[FallbackDim];
        foreach (Match m in Regex.Matches(text.ToLowerInvariant(), "[a-z0-9]+"))
            v[(int)(Fnv(m.Value) % FallbackDim)] += 1;
        var norm = MathF.Sqrt(v.Sum(x => x * x));
        if (norm > 0) for (int i = 0; i < v.Length; i++) v[i] /= norm;
        return v;
    }

    static uint Fnv(string s)
    {
        uint h = 2166136261;
        foreach (var c in s) { h ^= c; h *= 16777619; }
        return h;
    }

    public static double Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return na == 0 || nb == 0 ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}

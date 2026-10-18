using System.Text.Json;
using DocClassify.Api.Data;
using DocClassify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocClassify.Api.Services;

/// <summary>Minimal RAG retrieval layer: chunk, embed, store, cosine top-K. Swap for a vector DB at scale.</summary>
public class VectorSearch(AppDb db, EmbeddingService emb)
{
    public static IEnumerable<string> Chunk(string text, int size = 500, int overlap = 80)
    {
        text = text.Trim();
        for (int i = 0; i < text.Length; i += size - overlap)
        {
            yield return text.Substring(i, Math.Min(size, text.Length - i));
            if (i + size >= text.Length) yield break;
        }
    }

    public async Task IndexAsync(Document doc)
    {
        foreach (var piece in Chunk(doc.Content))
        {
            var v = await emb.EmbedAsync(piece);
            db.Chunks.Add(new Chunk { DocumentId = doc.Id, Text = piece, EmbeddingJson = JsonSerializer.Serialize(v) });
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<SearchHit>> SearchAsync(string query, int topK)
    {
        var q = await emb.EmbedAsync(query);
        var chunks = await db.Chunks.Include(c => c.Document).AsNoTracking().ToListAsync();
        return chunks
            .Select(c => (c, v: JsonSerializer.Deserialize<float[]>(c.EmbeddingJson)!))
            .Where(x => x.v.Length == q.Length)
            .Select(x => new SearchHit(x.c.DocumentId, x.c.Document!.FileName, x.c.Document.Bucket,
                Math.Round(EmbeddingService.Cosine(q, x.v), 4), x.c.Text))
            .OrderByDescending(h => h.Score)
            .Take(Math.Clamp(topK, 1, 20))
            .ToList();
    }
}

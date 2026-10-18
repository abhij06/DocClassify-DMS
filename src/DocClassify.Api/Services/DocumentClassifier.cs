using System.Text.Json;

namespace DocClassify.Api.Services;

public record Classification(string Bucket, double Confidence, string By);

/// <summary>LLM zero-shot classification into configured buckets with a keyword fallback.</summary>
public class DocumentClassifier(LlmService llm, IConfiguration cfg)
{
    static readonly Dictionary<string, string[]> Keywords = new()
    {
        ["Invoice"] = new[] { "invoice", "gst", "bill to", "amount due", "tax invoice" },
        ["KYC"] = new[] { "aadhaar", "pan", "passport", "voter id", "date of birth" },
        ["BankStatement"] = new[] { "statement", "balance", "debit", "credit", "account number" },
        ["LoanAgreement"] = new[] { "loan", "emi", "borrower", "lender", "interest rate" }
    };

    string[] Buckets => cfg.GetSection("Buckets").Get<string[]>() ?? new[] { "Other" };

    public async Task<Classification> ClassifyAsync(string content)
    {
        var buckets = Buckets;
        var snippet = content.Length > 3000 ? content[..3000] : content;
        var reply = await llm.ChatAsync(
            $"Classify the document into exactly one of: {string.Join(", ", buckets)}. " +
            "Reply with JSON only: {\"bucket\":\"<name>\",\"confidence\":<0..1>}", snippet);

        if (reply is not null)
        {
            try
            {
                int s = reply.IndexOf('{'), e = reply.LastIndexOf('}');
                using var doc = JsonDocument.Parse(reply[s..(e + 1)]);
                var bucket = doc.RootElement.GetProperty("bucket").GetString() ?? "Other";
                var conf = doc.RootElement.TryGetProperty("confidence", out var c) ? c.GetDouble() : 0.5;
                var match = buckets.FirstOrDefault(x => x.Equals(bucket, StringComparison.OrdinalIgnoreCase));
                if (match is not null) return new Classification(match, conf, "llm");
            }
            catch { /* fall through to keywords */ }
        }
        return Keyword(content);
    }

    static Classification Keyword(string content)
    {
        var lower = content.ToLowerInvariant();
        var best = Keywords.Select(k => (k.Key, Hits: k.Value.Count(w => lower.Contains(w))))
                           .OrderByDescending(x => x.Hits).First();
        return best.Hits == 0
            ? new Classification("Other", 0.2, "keywords")
            : new Classification(best.Key, Math.Min(0.9, 0.3 + 0.15 * best.Hits), "keywords");
    }
}

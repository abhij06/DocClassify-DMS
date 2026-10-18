using DocClassify.Api.Data;
using DocClassify.Api.Models;

namespace DocClassify.Api.Services;

/// <summary>Workflow: Uploaded -> Classified -> Indexed -> Reviewed, every transition recorded.</summary>
public class DocumentPipeline(AppDb db, DocumentClassifier classifier, VectorSearch search)
{
    public async Task<Document> IngestAsync(string fileName, string content)
    {
        var doc = new Document { FileName = fileName, Content = content };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();
        await Move(doc, DocState.Uploaded, "Received");

        var c = await classifier.ClassifyAsync(content);
        doc.Bucket = c.Bucket; doc.Confidence = c.Confidence; doc.ClassifiedBy = c.By;
        await Move(doc, DocState.Classified, $"{c.Bucket} ({c.Confidence:P0}) via {c.By}");

        await search.IndexAsync(doc);
        await Move(doc, DocState.Indexed, "Chunks embedded");
        return doc;
    }

    public async Task<Document?> ReviewAsync(int id, string? overrideBucket, string? note)
    {
        var doc = await db.Documents.FindAsync(id);
        if (doc is null) return null;
        if (!string.IsNullOrWhiteSpace(overrideBucket)) { doc.Bucket = overrideBucket; doc.ClassifiedBy = "human"; }
        await Move(doc, DocState.Reviewed, note ?? "Reviewed");
        return doc;
    }

    async Task Move(Document doc, DocState to, string note)
    {
        var from = doc.State;
        doc.State = to;
        db.Events.Add(new WorkflowEvent { DocumentId = doc.Id, FromState = from, ToState = to, Note = note });
        await db.SaveChangesAsync();
    }
}

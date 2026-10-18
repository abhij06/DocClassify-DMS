namespace DocClassify.Api.Models;

public enum DocState { Uploaded, Classified, Indexed, Reviewed }

public class Document
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public string Content { get; set; } = "";
    public string Bucket { get; set; } = "Other";
    public double Confidence { get; set; }
    public string ClassifiedBy { get; set; } = "";
    public DocState State { get; set; } = DocState.Uploaded;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Chunk
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document? Document { get; set; }
    public string Text { get; set; } = "";
    public string EmbeddingJson { get; set; } = "[]";
}

public class WorkflowEvent
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public DocState FromState { get; set; }
    public DocState ToState { get; set; }
    public string Note { get; set; } = "";
    public DateTime At { get; set; } = DateTime.UtcNow;
}

public record ReviewRequest(string? OverrideBucket, string? Note);
public record SearchRequest(string Query, int TopK = 5);
public record SearchHit(int DocumentId, string FileName, string Bucket, double Score, string Text);

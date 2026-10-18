using DocClassify.Api.Data;
using DocClassify.Api.Models;
using DocClassify.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocClassify.Api.Controllers;

[ApiController]
[Route("api")]
public class DocumentsController(DocumentPipeline pipe, VectorSearch search, AppDb db) : ControllerBase
{
    /// <summary>Upload a plain-text document (.txt, .md, .csv).</summary>
    [HttpPost("documents")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file.Length == 0 || file.Length > 2_000_000) return BadRequest("Empty file or larger than 2 MB");
        using var sr = new StreamReader(file.OpenReadStream());
        var d = await pipe.IngestAsync(file.FileName, await sr.ReadToEndAsync());
        return Ok(new { d.Id, d.FileName, d.Bucket, d.Confidence, d.ClassifiedBy, d.State });
    }

    [HttpGet("documents")]
    public async Task<IActionResult> List([FromQuery] string? bucket) =>
        Ok(await db.Documents.AsNoTracking()
            .Where(d => bucket == null || d.Bucket == bucket)
            .OrderByDescending(d => d.Id)
            .Select(d => new { d.Id, d.FileName, d.Bucket, d.Confidence, d.ClassifiedBy, d.State, d.CreatedAt })
            .ToListAsync());

    [HttpGet("documents/{id:int}/history")]
    public async Task<IActionResult> History(int id) =>
        Ok(await db.Events.AsNoTracking().Where(e => e.DocumentId == id).OrderBy(e => e.Id).ToListAsync());

    [HttpPost("documents/{id:int}/review")]
    public async Task<IActionResult> Review(int id, ReviewRequest r)
    {
        var d = await pipe.ReviewAsync(id, r.OverrideBucket, r.Note);
        return d is null ? NotFound() : Ok(new { d.Id, d.Bucket, d.State });
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search(SearchRequest r) =>
        string.IsNullOrWhiteSpace(r.Query) ? BadRequest("Query required") : Ok(await search.SearchAsync(r.Query, r.TopK));
}

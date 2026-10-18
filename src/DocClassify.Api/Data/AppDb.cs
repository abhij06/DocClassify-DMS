using DocClassify.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocClassify.Api.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<WorkflowEvent> Events => Set<WorkflowEvent>();
}

using FacetApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FacetApi.Data;

public class FacetDbContext : DbContext
{
    public DbSet<Document> Documents => Set<Document>();
    public FacetDbContext(DbContextOptions<FacetDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>().ToTable("Documents");
        modelBuilder.Entity<Document>().HasIndex(d => d.UploadedAt);
        base.OnModelCreating(modelBuilder);
    }
}

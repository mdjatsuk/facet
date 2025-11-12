using FacetApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FacetApi.Data;

public class FacetDbContext : DbContext
{
    public DbSet<Document> Documents => Set<Document>();
     public DbSet<User>? UserList { get; set; }
    public FacetDbContext(DbContextOptions<FacetDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>().ToTable("Documents");
        modelBuilder.Entity<Document>().HasIndex(d => d.UploadedAt);
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "testuser",
            Password = "testpass"
        });
        base.OnModelCreating(modelBuilder);
    }
}

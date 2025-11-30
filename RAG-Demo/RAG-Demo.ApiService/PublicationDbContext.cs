using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using RAG_Demo.Common;

namespace RAG_Demo.ApiService;

public class PublicationDbContext : DbContext
{
    public PublicationDbContext(DbContextOptions<PublicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Publication> Publications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Publication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Summary).IsRequired();
            entity.Property(e => e.CompanyDescription).IsRequired();
            entity.Property(e => e.Brand).IsRequired();
            entity.Property(e => e.Function).IsRequired();
            entity.Property(e => e.EmploymentLevel).IsRequired();
            entity.Property(e => e.EducationLevel).IsRequired();
            entity.Property(e => e.CompanyName).IsRequired();
            entity.Property(e => e.City).IsRequired();
            entity.Property(e => e.Embedding).HasColumnType("vector(1536)");
        });
    }
}

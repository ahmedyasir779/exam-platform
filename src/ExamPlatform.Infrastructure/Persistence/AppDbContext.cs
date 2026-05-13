using ExamPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamPlatform.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Answer> Answers => Set<Answer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ProcessedStatus).HasDefaultValue("pending");
            e.HasMany(x => x.Chunks).WithOne(x => x.Document)
                .HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chunk>(e => e.HasKey(x => x.Id));

        modelBuilder.Entity<Template>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StructureJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Exam>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Questions).WithOne(x => x.Exam)
                .HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Options).HasColumnType("jsonb");
            e.Property(x => x.SourceBbox).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Submission>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Answers).WithOne(x => x.Submission)
                .HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Answer>(e => e.HasKey(x => x.Id));
    }
}

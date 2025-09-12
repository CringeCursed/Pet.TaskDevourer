using Microsoft.EntityFrameworkCore;

namespace Pet.TaskDevourer.Api
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
        public DbSet<SubTaskEntity> SubTasks => Set<SubTaskEntity>();
        public DbSet<AttachmentEntity> Attachments => Set<AttachmentEntity>();
        public DbSet<TagEntity> Tags => Set<TagEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).IsRequired();
                e.HasMany(x => x.SubTasks).WithOne(x => x.Task!).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
                e.HasMany(x => x.Attachments).WithOne(x => x.Task!).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
                e.HasMany(x => x.Tags).WithOne(x => x.Task!).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<SubTaskEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<AttachmentEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<TagEntity>().HasKey(x => x.Id);
        }
    }

    public class TaskEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public List<TagEntity> Tags { get; set; } = new();
        public List<SubTaskEntity> SubTasks { get; set; } = new();
        public List<AttachmentEntity> Attachments { get; set; } = new();
    }

    public class SubTaskEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int TaskId { get; set; }
        public TaskEntity? Task { get; set; }
    }

    public class AttachmentEntity
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public long SizeBytes { get; set; }
        public int TaskId { get; set; }
        public TaskEntity? Task { get; set; }
    }

    public class TagEntity
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
        public int TaskId { get; set; }
        public TaskEntity? Task { get; set; }
    }
}

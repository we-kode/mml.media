using Media.DBContext.Extensions;
using Media.DBContext.Models;
using Microsoft.EntityFrameworkCore;

namespace Media.DBContext
{
  public class ApplicationDBContext : DbContext
  {
    public DbSet<Settings> Settings { get; set; } = null!;
    public DbSet<Records> Records { get; set; } = null!;
    public DbSet<Livestreams> Livestreams { get; set; } = null!;
    public DbSet<Artists> Artists { get; set; } = null!;
    public DbSet<Genres> Genres { get; set; } = null!;
    public DbSet<Albums> Albums { get; set; } = null!;
    public DbSet<Groups> Groups { get; set; } = null!;
    public DbSet<Languages> Languages { get; set; } = null!;
    public DbSet<SeedRecords> SeedRecords { get; set; } = null!;

    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.HasDefaultSchema("public");
      base.OnModelCreating(modelBuilder);

      foreach (var entity in modelBuilder.Model.GetEntityTypes())
      {
        var currentTableName = modelBuilder.Entity(entity.Name).Metadata.GetDefaultTableName();
        if (currentTableName!.Contains('<'))
        {
          currentTableName = currentTableName.Split('<')[0];
        }
        modelBuilder.Entity(entity.Name).ToTable(currentTableName.ToUnderscoreCase());
      }

      modelBuilder.Entity<Settings>()
       .HasIndex(s => s.Key)
       .IsUnique();

      modelBuilder
       .Entity<SeedRecords>()
       .HasNoKey();
    }
  }
}

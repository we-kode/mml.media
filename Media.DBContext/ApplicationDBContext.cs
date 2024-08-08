using Media.DBContext.Extensions;
using Media.DBContext.Models;
using Microsoft.EntityFrameworkCore;

namespace Media.DBContext
{
  public class ApplicationDBContext : DbContext
  {
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<Record> Records { get; set; } = null!;
    public DbSet<Livestream> Livestreams { get; set; } = null!;
    public DbSet<Artist> Artists { get; set; } = null!;
    public DbSet<Genre> Genres { get; set; } = null!;
    public DbSet<Album> Albums { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<Language> Languages { get; set; } = null!;
    public DbSet<SeedRecord> SeedRecords { get; set; } = null!;

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
        var currentTableName = modelBuilder.Entity(entity.Name).Metadata.GetTableName();
        if (currentTableName!.Contains('<'))
        {
          currentTableName = currentTableName.Split('<')[0];
        }
        modelBuilder.Entity(entity.Name).ToTable(currentTableName.ToUnderscoreCase());
      }

      modelBuilder
       .Entity<Group>()
       .HasMany(g => g.Records)
       .WithMany(rec => rec.Groups)
       .UsingEntity(e => e.ToTable("groups_records"));

      modelBuilder
       .Entity<Group>()
       .HasMany(g => g.Livestreams)
       .WithMany(l => l.Groups)
       .UsingEntity(e => e.ToTable("groups_livestreams"));

      modelBuilder
       .Entity<Setting>()
       .HasIndex(s => s.Key)
       .IsUnique();

      modelBuilder
       .Entity<SeedRecord>()
       .HasNoKey();
    }
  }
}

using Media.DBContext.Extensions;
using Media.DBContext.Models;
using Microsoft.EntityFrameworkCore;

namespace Media.DBContext
{
  public class ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : DbContext(options)
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
    public DbSet<FederationPeer> FederationPeers { get; set; } = null!;
    public DbSet<FederationRule> FederationRules { get; set; } = null!;
    public DbSet<FederationRecordLink> FederationRecordLinks { get; set; } = null!;
    public DbSet<FederationAuditEntry> FederationAuditLog { get; set; } = null!;
    public DbSet<FederationJob> FederationJobs { get; set; } = null!;
    public DbSet<FederationInvitation> FederationInvitations { get; set; } = null!;

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

      modelBuilder
       .Entity<FederationPeer>()
       .HasIndex(p => p.Instance)
       .IsUnique();

      modelBuilder
       .Entity<FederationRule>()
       .HasMany(r => r.Groups)
       .WithMany()
       .UsingEntity(e => e.ToTable("federation_rules_groups"));

      modelBuilder
       .Entity<FederationRule>()
       .HasMany(r => r.Peers)
       .WithMany(p => p.Rules)
       .UsingEntity(e => e.ToTable("federation_rules_peers"));

      modelBuilder
       .Entity<FederationRule>()
       .HasMany(r => r.Artists)
       .WithMany()
       .UsingEntity(e => e.ToTable("federation_rules_artists"));

      modelBuilder
       .Entity<FederationRule>()
       .HasMany(r => r.Albums)
       .WithMany()
       .UsingEntity(e => e.ToTable("federation_rules_albums"));

      modelBuilder
       .Entity<FederationRule>()
       .HasMany(r => r.Genres)
       .WithMany()
       .UsingEntity(e => e.ToTable("federation_rules_genres"));

      modelBuilder
       .Entity<FederationRecordLink>()
       .HasIndex(l => new { l.PeerId, l.LocalRecordId })
       .IsUnique();

      modelBuilder
       .Entity<FederationRecordLink>()
       .HasIndex(l => new { l.PeerId, l.TransactionId })
       .IsUnique();

      modelBuilder
       .Entity<FederationRecordLink>()
       .HasOne(l => l.LocalRecord)
       .WithMany()
       .HasForeignKey(l => l.LocalRecordId)
       .OnDelete(DeleteBehavior.Cascade);

      modelBuilder
       .Entity<FederationAuditEntry>()
       .HasIndex(a => a.Timestamp);

      modelBuilder
       .Entity<FederationJob>()
       .HasIndex(j => j.NextAttemptAt);

      modelBuilder
       .Entity<FederationInvitation>()
       .HasIndex(i => i.CodeHash)
       .IsUnique();
    }
  }
}

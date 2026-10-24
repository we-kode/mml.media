using Media.Application.Extensions;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Media.API.HostedServices;

internal class MigrateCovers(Func<ApplicationDBContext> contextFactory) : BackgroundService
{
  private readonly string coverPath = "/records/covers";
  private readonly string migrationID = "20260304000000_Migrate_Covers";
  private readonly string productVersion = "10.0.0";
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var ctx = contextFactory();
    // check if migration is already applied
    var appliedMigrations = await ctx.Database.GetAppliedMigrationsAsync(stoppingToken);
    if (appliedMigrations.Contains(migrationID))
    {
      return;
    }

    // get all records with covers
    var records = await ctx.Records.Where(r => !string.IsNullOrWhiteSpace(r.Cover)).ToListAsync(stoppingToken);

    // extract covers from base64 and save them to disk with short hash as filename
    foreach (var record in records)
    {
      byte[] coverData = [];
      if (!Convert.TryFromBase64String(record.Cover!, coverData, out _))
      {
        continue; 
      }

      var fileName = coverData.ToFileName();
      var coverPathWithHash = $"{coverPath}/{fileName}";
      await File.WriteAllBytesAsync(coverPathWithHash, coverData, stoppingToken);
      record.Cover = fileName;
    }

    // update records with new cover path
    await ctx.SaveChangesAsync(stoppingToken);

    // apply migration
    await ctx.Database.ExecuteSqlRawAsync(
        "INSERT INTO \"__EFMigrationsHistory\" (\"migration_id\", \"product_version\") VALUES ({0}, {1})",
        migrationID, productVersion);
  }
}

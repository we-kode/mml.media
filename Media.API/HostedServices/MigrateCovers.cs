using Media.Application.Contracts.IO;
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

/// <summary>
/// Moves the base64 covers of records into cover files once on startup.
/// </summary>
public class MigrateCovers(Func<ApplicationDBContext> contextFactory, ICoverLoader coverLoader) : BackgroundService
{
  private const int BatchSize = 100;
  private const string MigrationID = "20261110000000_Migrate_Covers_To_Files";
  private const string ProductVersion = "10.0.0";

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var ctx = contextFactory();
    var appliedMigrations = await ctx.Database.GetAppliedMigrationsAsync(stoppingToken);
    if (appliedMigrations.Contains(MigrationID))
    {
      return;
    }

    var ids = await ctx.Records
      .Where(r => r.Cover != null && r.Cover != string.Empty)
      .Select(r => r.RecordId)
      .ToListAsync(stoppingToken);

    var failed = 0;
    foreach (var chunk in ids.Chunk(BatchSize))
    {
      var records = await ctx.Records.Where(r => chunk.Contains(r.RecordId)).ToListAsync(stoppingToken);
      foreach (var record in records)
      {
        try
        {
          record.Cover = await MigrateCover(record.Cover!) ?? record.Cover;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
          failed++;
          Console.WriteLine($"Migration of the cover of record {record.RecordId} failed: {e.Message}");
        }
      }

      await ctx.SaveChangesAsync(stoppingToken);
      ctx.ChangeTracker.Clear();
    }

    if (failed > 0)
    {
      Console.WriteLine($"Migration of {failed} covers failed. The migration is repeated on the next start.");
      return;
    }

    await ctx.Database.ExecuteSqlRawAsync(
        "INSERT INTO \"__EFMigrationsHistory\" (\"migration_id\", \"product_version\") VALUES ({0}, {1})",
        [MigrationID, ProductVersion],
        stoppingToken);
  }

  /// <summary>
  /// Stores a base64 cover as cover file.
  /// </summary>
  /// <param name="cover">The stored cover of a record.</param>
  /// <returns>The file name of the stored cover or null, if the cover is no base64 cover.</returns>
  public async Task<string?> MigrateCover(string cover)
  {
    if (cover.IsCoverFileName())
    {
      return null;
    }

    byte[] data;
    try
    {
      data = Convert.FromBase64String(cover);
    }
    catch (FormatException)
    {
      return null;
    }

    if (data.Length == 0)
    {
      return null;
    }

    var fileName = data.ToFileName();
    await coverLoader.Save(data, fileName);
    return fileName;
  }
}

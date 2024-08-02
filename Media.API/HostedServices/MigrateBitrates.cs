using FFmpeg.NET;
using Media.Application.Contracts.Repositories;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Media.API.HostedServices;

public class MigrateBitrates(ISettingRepository _settings, IGenreRepository _genres) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var isMigrated = _settings.Get("MigrationBitrates", bool.FalseString);
    if (!bool.Parse(isMigrated))
    {
      // migrate bitrate for all existing files
      Console.WriteLine("Start migration of bitrate indexing...");
      Engine engine = new Engine($"/usr/bin/ffmpeg");
      var path = @$"/records/";
      var files = Directory.GetFiles(path);
      for (int i = 0; i < files.Length; ++i)
      {
        var filePath = files[i];
        Console.WriteLine($"Get Bitrate of {filePath} ({i}/{files.Length})");
        var file = new InputFile(filePath);
        var fileMetaData = engine.GetMetaDataAsync(file, default).Result;
        await _genres.UpdateBitrate(file.FileInfo.Name, fileMetaData.AudioData.BitRateKbs).ConfigureAwait(false);
      }
      Console.WriteLine("Bitrates of existing files indexed.");
    }
    _settings.Save("MigrationBitrates", bool.TrueString);
  }
}

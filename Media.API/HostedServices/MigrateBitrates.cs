﻿using FFmpeg.NET;
using Media.Application.Contracts;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Media.API.HostedServices
{
  public class MigrateBitrates : BackgroundService
  {

    private ISettingsRepository _settings;
    private IRecordsRepository _records;

    public MigrateBitrates(ISettingsRepository settings, IRecordsRepository records)
    {
      _settings = settings;
      _records = records;
    }

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
          await _records.UpdateBitrate(file.FileInfo.Name, fileMetaData.AudioData.BitRateKbs).ConfigureAwait(false);
        }
        Console.WriteLine("Bitrates of existing files indexed.");
      }
      _settings.Save("MigrationBitrates", bool.TrueString);
    }
  }
}

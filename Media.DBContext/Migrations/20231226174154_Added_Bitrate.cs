using FFmpeg.NET;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;

#nullable disable

namespace Media.DBContext.Migrations
{
  public partial class Added_Bitrate : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<int>(
          name: "bitrate",
          schema: "public",
          table: "records",
          type: "integer",
          nullable: true);

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
        migrationBuilder.Sql(@$"UPDATE public.records SET bitrate = {fileMetaData.AudioData.BitRateKbs} WHERE checksum = '{file.FileInfo.Name}';");
      }
      Console.WriteLine("Bitrates of existing files indexed.");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "bitrate",
          schema: "public",
          table: "records");
    }
  }
}

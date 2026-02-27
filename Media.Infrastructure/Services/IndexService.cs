using ByteDev.Crypto;
using ByteDev.Crypto.Hashing;
using ByteDev.Crypto.Hashing.Algorithms;
using FFmpeg.NET;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Extensions;
using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Media.Infrastructure.Services;

public class IndexService(ISettingRepository settingsRepository,
  IRecordRepository recordRepository,
  IRecordService recordsService,
  IGenreRepository genresRepository) : IIndexService
{

  private readonly Engine engine = new($"/usr/bin/ffmpeg");
  private readonly FileChecksumService checksumService = new(new Sha1Algorithm(), EncodingType.Hex);
  private const string CompressionRateKey = "CompressionRate";

  public async Task IndexFile(string fileName, DateTime modifiedAt, List<Guid> groups)
  {
    var inputPath = @$"/tmp/records/{fileName}";
    var inputFile = new InputFile(inputPath);

    if (!File.Exists(inputPath))
    {
      return;
    }

    // calculate new file name.
    var outputFileName = checksumService.Calculate(inputPath);
    var outputPath = @$"/records/";
    var outputFilePath = $"{outputPath}{outputFileName}";
    var outputFile = new OutputFile(outputFilePath);

    // if file is indexed already skip
    if (File.Exists($"{outputPath}{outputFileName}") && recordRepository.IsIndexed(outputFileName))
    {
      DeleteFile(inputPath);
      return;
    }

    // get id3 tags and remove them from original file.
    var taglibFile = TagLib.File.Create(inputPath);
    var originalFileName = Path.GetFileNameWithoutExtension(fileName);
    var trackNumber = (int)taglibFile.Tag.Track;
    var isDateParsed = DateTime.TryParseExact(originalFileName.Split('-').FirstOrDefault(), "yyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedDate);
    var cover = taglibFile.Tag.Pictures.FirstOrDefault();
    string? coverBase64 = null;
    if (cover != null)
    {
      coverBase64 = Convert.ToBase64String(cover.Data.Data);
    }

    var metadata = new RecordMetaData
    {
      Title = taglibFile.Tag.Title,
      Artist = taglibFile.Tag.FirstPerformer,
      Album = taglibFile.Tag.Album,
      Genre = taglibFile.Tag.FirstGenre,
      Language = taglibFile.LanguageTag(),
      TrackNumber = trackNumber,
      Date = isDateParsed ? parsedDate.ToUniversalTime().AddMinutes(trackNumber) : modifiedAt.ToUniversalTime(),
      Duration = taglibFile.Properties.Duration,
      OriginalFileName = originalFileName,
      PhysicalFilePath = outputPath,
      Checksum = outputFileName,
      Cover = coverBase64,
    };
    taglibFile.RemoveTags(TagLib.TagTypes.AllTags);
    taglibFile.Save();
    taglibFile.Dispose();

    var compressionRate = genresRepository.Bitrate(metadata.Genre);

    if (!compressionRate.HasValue && int.TryParse(settingsRepository.Get(CompressionRateKey, ""), out var defaultCompressionRate))
    {
      compressionRate = defaultCompressionRate;
    }

    var fileMetaData = await engine.GetMetaDataAsync(inputFile, default).ConfigureAwait(false);
    if (compressionRate.HasValue && fileMetaData.AudioData.BitRateKbs > compressionRate.Value)
    {
      // compress and write file to output
      var conversionOptions = new ConversionOptions
      {

        AudioBitRate = compressionRate,
        ExtraArguments = "-f mp3"
      };
      await engine.ConvertAsync(inputFile, outputFile, conversionOptions, default).ConfigureAwait(false);
    }
    else
    {
      File.Copy(inputPath, outputFilePath, true);
    }

    // remove original file
    DeleteFile(inputPath);
    metadata.Bitrate = compressionRate ?? fileMetaData.AudioData.BitRateKbs;

    // save indexed file
    await recordsService.SaveMetaData(metadata, groups);
  }

  /// <summary>
  /// Removes temporary file from system
  /// </summary>
  /// <param name="filePath">File to be deleted.</param>
  private static void DeleteFile(string filePath)
  {
    if (File.Exists(filePath))
    {
      File.Delete(filePath);
    }
  }
}

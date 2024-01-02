using ByteDev.Crypto;
using ByteDev.Crypto.Hashing;
using ByteDev.Crypto.Hashing.Algorithms;
using FFmpeg.NET;
using MassTransit;
using Media.Application.Contracts;
using Media.Application.Models;
using Media.Messages;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Media.Application.Extensions;
using MassTransit.DependencyInjection;
using static System.Net.Mime.MediaTypeNames;

namespace Media.Application.Consumers;

/// <summary>
/// Indexes one uploaded file.
/// </summary>
public class IndexingRecordConsumer : IConsumer<FileUploaded>
{
  private readonly Engine engine = new Engine($"/usr/bin/ffmpeg");
  private readonly IFileChecksumService checksumService = new FileChecksumService(new Sha1Algorithm(), EncodingType.Hex);
  private readonly ISettingsRepository settingsRepository;
  private readonly IRecordsRepository recordsRepository;

  public IndexingRecordConsumer(ISettingsRepository settingsRepository, IRecordsRepository recordsRepository)
  {
    this.settingsRepository = settingsRepository;
    this.recordsRepository = recordsRepository;
  }

  public async Task Consume(ConsumeContext<FileUploaded> context)
  {
    var inputPath = @$"/tmp/records/{context.Message.FileName}";
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
    if (File.Exists($"{outputPath}{outputFileName}") && recordsRepository.IsIndexed(outputFileName))
    {
      DeleteFile(inputPath);
      return;
    }

    // get id3 tags and remove them from original file.
    var taglibFile = TagLib.File.Create(inputPath);
    var originalFileName = Path.GetFileNameWithoutExtension(context.Message.FileName);
    var trackNumber = (int)taglibFile.Tag.Track;
    var isDateParsed = DateTime.TryParseExact(originalFileName.Split('-').FirstOrDefault(), "yyMMdd", CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedDate);
    var metadata = new RecordMetaData
    {
      Title = taglibFile.Tag.Title,
      Artist = taglibFile.Tag.FirstPerformer,
      Album = taglibFile.Tag.Album,
      Genre = taglibFile.Tag.FirstGenre,
      Language = taglibFile.LanguageTag(),
      TrackNumber = trackNumber,
      Date = isDateParsed ? parsedDate.ToUniversalTime().AddMinutes(trackNumber) : context.Message.Date.ToUniversalTime(),
      Duration = taglibFile.Properties.Duration,
      OriginalFileName = originalFileName,
      PhysicalFilePath = outputPath,
      Checksum = outputFileName
    };
    taglibFile.RemoveTags(TagLib.TagTypes.AllTags);
    taglibFile.Save();
    taglibFile.Dispose();

    var compressionRate = recordsRepository.Bitrate(metadata.Genre);

    if (!compressionRate.HasValue && int.TryParse(settingsRepository.Get(Constants.Settings.CompressionRateKey, ""), out var defaultCompressionRate))
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
    recordsRepository.SaveMetaData(metadata, context.Message.Groups);
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

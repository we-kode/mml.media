using ByteDev.Crypto;
using ByteDev.Crypto.Hashing;
using ByteDev.Crypto.Hashing.Algorithms;
using FFmpeg.NET;
using MassTransit;
using Media.Application.Contracts;
using Media.Application.Models;
using Media.Messages;
using System.IO;
using System.Threading.Tasks;

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
  private readonly IGroupRepository groupRepository;

  public IndexingRecordConsumer(ISettingsRepository settingsRepository, IRecordsRepository recordsRepository, IGroupRepository groupRepository)
  {
    this.settingsRepository = settingsRepository;
    this.recordsRepository = recordsRepository;
    this.groupRepository = groupRepository;
  }

  public async Task Consume(ConsumeContext<FileUploaded> context)
  {
    var inputPath = @$"/tmp/records/{context.Message.FileName}";
    var inputFile = new InputFile(inputPath);

    // calculate new file name.
    var outputFileName = checksumService.Calculate(inputPath);
    var outputPath = @$"/records/";
    var outputFile = new OutputFile($"{outputPath}{outputFileName}");

    // if file is indexed already skip
    if (File.Exists($"{outputPath}{outputFileName}") && recordsRepository.IsIndexed(outputFileName))
    {
      DeleteFile(inputPath);
      return;
    }

    // get id3 tags and remove them from original file.
    var taglibFile = TagLib.File.Create(inputPath);
    var metadata = new RecordMetaData
    {
      Title = taglibFile.Tag.Title,
      Artist = taglibFile.Tag.FirstPerformer,
      Album = taglibFile.Tag.Album,
      Genre = taglibFile.Tag.FirstGenre,
      TrackNumber = (int)taglibFile.Tag.Track,
      Date = context.Message.Date.ToUniversalTime().Date,
      OriginalFileName = Path.GetFileNameWithoutExtension(context.Message.FileName),
      PhysicalFilePath = outputPath,
      Checksum = outputFileName,
      DefaultGroupId = groupRepository.GetDefaultGroup()
    };
    taglibFile.RemoveTags(TagLib.TagTypes.AllTags);
    taglibFile.Save();
    taglibFile.Dispose();

    // compress and write file to output
    var conversionOptions = new ConversionOptions
    {
      AudioBitRate = int.Parse(settingsRepository.Get(ISettingsRepository.CompressionRateKey, "96")),
      ExtraArguments = "-f mp3"
    };
    await engine.ConvertAsync(inputFile, outputFile, conversionOptions, default).ConfigureAwait(false);

    // remove original file
    DeleteFile(inputPath);

    // save indexed file
    recordsRepository.SaveMetaData(metadata);
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

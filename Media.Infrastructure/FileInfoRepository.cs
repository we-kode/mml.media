using Media.Application.Contracts;
using Media.Application.Models;
using System.IO;
using System.Linq;

namespace Media.Infrastructure;

public class FileInfoRepository : IInfoRepository
{

  private const string rootPath = @$"/info/";

  public string Content(string path)
  {
    var fullPath = Path.Combine(rootPath, path ?? "").Replace("..", "");
    return File.ReadAllText(fullPath);
  }

  public bool ExistsFolder(string path)
  {
    var fullPath = Path.Combine(rootPath, path ?? "").Replace("..", "");
    return Directory.Exists(fullPath);
  }

  public bool ExistsFile(string path)
  {
    var fullPath = Path.Combine(rootPath, path ?? "").Replace("..", "");
    return File.Exists(fullPath);
  }

  public Infos List(string? path, int skip = Application.Constants.List.Skip, int take = Application.Constants.List.Take)
  {
    var fullPath = Path.Combine(rootPath,path ?? "").Replace("..", "");
    var entries = Directory.GetFileSystemEntries(fullPath);

    var count = entries.Count();  

    var infos = entries
    .Skip(skip)
    .Take(take)
    .Select(path => new Info
    {
      Name = new FileInfo(path).Name,
      Path = Path.GetRelativePath(rootPath, path),
      IsFolder = new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory)
    });

    return new Infos
    {
      TotalCount = entries.Count(),
      Items = infos.ToList()
    };
  }
}

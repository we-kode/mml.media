using Media.Application.Contracts.IO;
using Media.Application.Extensions;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Media.Infrastructure.IO;

public class CoverLoader(IMemoryCache cache) : ICoverLoader
{
  private readonly string coverPath = "/records/covers";

  public async Task<byte[]?> Load(string fileName)
  {
    if (!fileName.IsCoverFileName() || !File.Exists(Path.Combine(coverPath, fileName)))
    {
      return null;
    }

    if (cache.TryGetValue(fileName, out byte[]? cachedData))
    {
      return cachedData;
    }

    var file = await File.ReadAllBytesAsync(Path.Combine(coverPath, fileName));

    var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(60));
    cache.CreateEntry(fileName).SetOptions(cacheEntryOptions).SetValue(file);

    return file;
  }

  public async Task Save(byte[] data, string fileName)
  {
    if (File.Exists(Path.Combine(coverPath, fileName)))
    {
      return;
    }

    await File.WriteAllBytesAsync(Path.Combine(coverPath, fileName), data);
  }

  public void Delete(string fileName)
  {
    if (!fileName.IsCoverFileName())
    {
      return;
    }

    cache.Remove(fileName);
    File.Delete(Path.Combine(coverPath, fileName));
  }

  public IEnumerable<string> List(DateTime writtenBefore)
  {
    return new DirectoryInfo(coverPath)
      .EnumerateFiles()
      .Where(file => file.LastWriteTimeUtc < writtenBefore.ToUniversalTime() && file.Name.IsCoverFileName())
      .Select(file => file.Name);
  }
}

using Media.Application.Contracts.IO;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Media.Infrastructure.IO;

public class CoverLoader(IMemoryCache cache) : ICoverLoader
{
  private readonly string coverPath = "/records/covers";

  public async Task<byte[]?> Load(string fileName)
  {
    if (!File.Exists(Path.Combine(coverPath, fileName)))
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
}

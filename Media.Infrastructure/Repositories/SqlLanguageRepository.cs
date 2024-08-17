using AutoMapper;
using Media.Application.Contracts.Repositories;
using Media.Application.Models;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Media.Infrastructure.Repositories;

public class SqlLanguageRepository(Func<ApplicationDBContext> contextFactory, IMapper mapper) : ILanguageRepository
{
  public Languages ListLanguages(string? filter, bool filterByGroups, IEnumerable<Guid> clientGroups, int skip = 0, int take = 100)
  {
    using var context = contextFactory();
    var query = context.Languages
     .Where(al => string.IsNullOrEmpty(filter) || EF.Functions.ILike(al.Name, $"%{filter}%"));

    if (filterByGroups)
    {
      query = query.Where(album => album.Records.Any(rec => rec.Groups.Any(g => clientGroups.Contains(g.GroupId))));
    }

    var count = query.Count();
    var langs = query
      .OrderBy(lang => lang.Name)
      .Skip(skip)
      .Take(take)
      .Select(lang => mapper.Map<Language>(lang))
      .ToList();

    return new Languages
    {
      TotalCount = count,
      Items = langs
    };
  }

  public async Task TryRemove(string? languageName)
  {
    using var context = contextFactory();

    if (string.IsNullOrEmpty(languageName))
    {
      return;
    }

    if (!context.Records.Any(rec => rec.Language != null && rec.Language.Name == languageName))
    {
      context.Languages.RemoveRange(context.Languages.Where(rec => rec.Name == languageName));
    }
    
    await context.SaveChangesAsync().ConfigureAwait(false);
  }

  public async Task<Language?> TryGetOrCreate(string? languageName)
  {
    using var context = contextFactory();

    if (string.IsNullOrEmpty(languageName))
    {
      return null;
    }

    var lang = context.Languages.FirstOrDefault(g => g.Name == languageName);
    lang ??= new DBContext.Models.Language
    {
      Name = languageName
    };


    if (lang.LanguageId == Guid.Empty)
    {
      context.Languages.Add(lang);
      await context.SaveChangesAsync().ConfigureAwait(false);
    }
    
    return mapper.Map<Language?>(lang);
  }
}

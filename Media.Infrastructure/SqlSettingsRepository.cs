using Media.Application.Contracts;
using Media.DBContext;
using System;
using System.Linq;

namespace Media.Infrastructure
{
  public class SqlSettingsRepository : ISettingsRepository
  {
    private readonly Func<ApplicationDBContext> _contextFactory;

    public SqlSettingsRepository(Func<ApplicationDBContext> contextFactory)
    {
      _contextFactory = contextFactory;
    }

    public string Get(string key, string defaultValue)
    {
      using var context = _contextFactory();
      return context.Settings.FirstOrDefault(s => s.Key == key)?.Value ?? defaultValue;
    }

    public void Save(string key, string value)
    {
      using var context = _contextFactory();
      var settings = context.Settings.FirstOrDefault(s => s.Key == key);
      if (settings == null)
      {
        settings = new DBContext.Models.Settings
        {
          Key = key,
          Value = value
        };
      }

      settings.Value = value;
      context.SaveChanges();
    }
  }
}

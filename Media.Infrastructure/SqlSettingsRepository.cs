using Media.Application.Contracts;
using Media.Application.Models;
using Media.DBContext;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

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

    public Settings Get()
    {
      using var context = _contextFactory();
      var dbSettings = context.Settings.ToDictionary(s => s.Key, s => s.Value);
      var settings = new Settings(); 
      foreach (var key in dbSettings.Keys)
      {
        Type type = typeof(Settings);
        // Get the PropertyInfo object by passing the property name.
        var propInfo = type.GetProperty(key);
        if (propInfo == null)
        {
          continue;
        }
        var converter = TypeDescriptor.GetConverter(propInfo.PropertyType);
        propInfo.SetValue(settings, converter.ConvertFromString(dbSettings[key]), null);
      }

      return settings;
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

    public void Save(Settings settings)
    {
      using var context = _contextFactory();
      var dict = settings.ToDictionary();
      foreach (var key in dict.Keys)
      {
        var dbSettings = context.Settings.FirstOrDefault(s => s.Key == key);
        if (dbSettings == null)
        {
          dbSettings = new DBContext.Models.Settings
          {
            Key = key
          };
          context.Add(dbSettings);
        }

        dbSettings.Value = dict[key];
      }

      context.SaveChanges();
    }
  }
}

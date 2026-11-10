using Media.API.Extensions;
using Media.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;

namespace Media.API.Filters;

/// <summary>
/// Removes the covers of records sent to clients requesting API version 1.0.
/// These apps, released before 3.0, expect covers as base64 instead of cover file names.
/// </summary>
public class LegacyClientCoverFilter : IResultFilter
{
  public void OnResultExecuting(ResultExecutingContext context)
  {
    if (context.HttpContext.GetRequestedApiVersion()?.MajorVersion != 1 || context.HttpContext.IsAdmin())
    {
      return;
    }

    IEnumerable<Record> records = (context.Result as ObjectResult)?.Value switch
    {
      Record record => [record],
      Records list => list.Items,
      IEnumerable<Record> list => list,
      _ => [],
    };

    foreach (var record in records)
    {
      record.Cover = null;
    }
  }

  public void OnResultExecuted(ResultExecutedContext context)
  {
  }
}

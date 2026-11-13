using Asp.Versioning;
using Media.API.Filters;
using Media.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;
using Record = Media.Application.Models.Record;

namespace Media.Test.Covers;

public class LegacyClientCoverFilterTests
{
  private const string Cover = "AbCdEfGhIjKlMnOp.jpg";

  private static Record CreateRecord() => new(Guid.NewGuid(), "Title", TestEnvironment.Instance) { Cover = Cover };

  private static void Execute(object value, int majorVersion, bool isAdmin = false)
  {
    var httpContext = new DefaultHttpContext();
    httpContext.ApiVersioningFeature.RequestedApiVersion = new ApiVersion(majorVersion, 0);
    if (isAdmin)
    {
      httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("role", "Admin")], "test"));
    }

    var context = new ResultExecutingContext(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), [], new ObjectResult(value), null!);
    new LegacyClientCoverFilter().OnResultExecuting(context);
  }

  [Fact]
  public void OnResultExecuting_RemovesCoversForLegacyClients()
  {
    var record = CreateRecord();
    var records = new Records { Items = [CreateRecord()] };
    var list = new List<Record> { CreateRecord() };

    Execute(record, 1);
    Execute(records, 1);
    Execute(list, 1);

    Assert.Null(record.Cover);
    Assert.Null(records.Items[0].Cover);
    Assert.Null(list[0].Cover);
  }

  [Fact]
  public void OnResultExecuting_KeepsCoversForAdmins()
  {
    var record = CreateRecord();

    Execute(record, 1, isAdmin: true);

    Assert.Equal(Cover, record.Cover);
  }

  [Fact]
  public void OnResultExecuting_KeepsCoversForCurrentClients()
  {
    var record = CreateRecord();

    Execute(record, 2);

    Assert.Equal(Cover, record.Cover);
  }
}

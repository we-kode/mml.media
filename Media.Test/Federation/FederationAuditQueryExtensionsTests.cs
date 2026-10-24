using Media.Application.Models.Federation;
using Media.Application.Types;
using Media.DBContext;
using Media.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Xunit;
using DBAuditEntry = Media.DBContext.Models.FederationAuditEntry;

namespace Media.Test.Federation;

public class FederationAuditQueryExtensionsTests
{
  private const string Local = "local";
  private static readonly DateTime Now = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

  private static DBAuditEntry Entry(string name, AuditAction action, AuditStatus status, string source, string target, int ageInHours, string? message = null) => new()
  {
    EntryId = Guid.NewGuid(),
    Timestamp = Now.AddHours(-ageInHours),
    Action = (int)action,
    Status = (int)status,
    Source = source,
    Target = target,
    Message = message ?? name,
  };

  private static readonly DBAuditEntry[] Entries =
  [
    Entry("pushed", AuditAction.RecordPushed, AuditStatus.Success, Local, "beta", 1),
    Entry("received", AuditAction.RecordReceived, AuditStatus.Skipped, "alpha", Local, 2),
    Entry("failed", AuditAction.RecordPushed, AuditStatus.Failed, Local, "alpha", 30, "Connection Refused"),
    Entry("keys", AuditAction.KeysChanged, AuditStatus.Success, "gamma", Local, 50),
  ];

  private static string[] Apply(AuditQuery query) =>
    [.. Entries.AsQueryable().Filter(query, Local).Sort(query, Local).Select(e => e.Message!)];

  [Fact]
  public void FilterAndSort_AreTranslatedToSql()
  {
    var options = new DbContextOptionsBuilder<ApplicationDBContext>().UseNpgsql("Host=localhost").Options;
    using var context = new ApplicationDBContext(options);
    var query = new AuditQuery
    {
      Filter = "alpha",
      Statuses = [AuditStatus.Failed],
      Actions = [AuditAction.RecordPushed],
      Direction = SyncDirection.Incoming,
      Instances = ["alpha"],
      From = Now.AddDays(-1),
      To = Now,
      Message = "refused",
      Sort = [new(AuditSortField.Instance, false), new(AuditSortField.Direction, true), new(AuditSortField.Status, false), new(AuditSortField.Action, true)],
    };

    var sql = context.FederationAuditLog.Filter(query, Local).Sort(query, Local).ToQueryString();

    Assert.Contains("lower(", sql);
    Assert.Contains("ORDER BY", sql);
  }

  [Fact]
  public void Filter_WithoutCriteria_ReturnsNewestFirst()
  {
    Assert.Equal(["pushed", "received", "Connection Refused", "keys"], Apply(new AuditQuery()));
  }

  [Fact]
  public void Filter_ByDirection_ComparesSourceWithLocalInstance()
  {
    Assert.Equal(["pushed", "Connection Refused"], Apply(new AuditQuery { Direction = SyncDirection.Outgoing }));
    Assert.Equal(["received", "keys"], Apply(new AuditQuery { Direction = SyncDirection.Incoming }));
  }

  [Fact]
  public void Filter_ByInstance_UsesThePartnerOfBothDirections()
  {
    Assert.Equal(["received", "Connection Refused"], Apply(new AuditQuery { Instances = ["alpha"] }));
    Assert.Equal(["pushed", "keys"], Apply(new AuditQuery { Instances = ["beta", "gamma"] }));
  }

  [Fact]
  public void Filter_CombinesStatusesActionsAndDates()
  {
    var query = new AuditQuery
    {
      Statuses = [AuditStatus.Success, AuditStatus.Failed],
      Actions = [AuditAction.RecordPushed, AuditAction.KeysChanged],
      From = Now.AddHours(-48),
      To = Now.AddHours(-1),
    };

    Assert.Equal(["Connection Refused"], Apply(query));
  }

  [Fact]
  public void Filter_ByMessage_IgnoresCase()
  {
    Assert.Equal(["Connection Refused"], Apply(new AuditQuery { Message = " refused " }));
  }

  [Fact]
  public void Sort_ByInstanceAscending_SortsEqualValuesNewestFirst()
  {
    var query = new AuditQuery { Sort = [new(AuditSortField.Instance, false)] };
    Assert.Equal(["received", "Connection Refused", "pushed", "keys"], Apply(query));
  }

  [Fact]
  public void Sort_ByStatusDescending()
  {
    var query = new AuditQuery { Sort = [new(AuditSortField.Status, true)] };
    Assert.Equal(["received", "Connection Refused", "pushed", "keys"], Apply(query));
  }

  [Fact]
  public void Sort_ByDirectionAscending_ListsIncomingFirst()
  {
    var query = new AuditQuery { Sort = [new(AuditSortField.Direction, false)] };
    Assert.Equal(["received", "keys", "pushed", "Connection Refused"], Apply(query));
  }

  [Fact]
  public void Sort_BySeveralColumns_UsesTheirPriority()
  {
    var query = new AuditQuery { Sort = [new(AuditSortField.Action, false), new(AuditSortField.Status, true)] };
    Assert.Equal(["keys", "Connection Refused", "pushed", "received"], Apply(query));
  }
}

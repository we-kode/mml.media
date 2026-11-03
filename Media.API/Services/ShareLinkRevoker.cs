using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Media.API.Services;

public class ShareLinkRevoker(
  IShareLinkRepository linkRepository,
  IShareRuleRepository ruleRepository,
  IRecordRepository recordRepository,
  IAuthorizationClient authClient) : IShareLinkRevoker
{
  public async Task RevokeUnallowed(string? clientId = null)
  {
    var revoked = new List<Guid>();
    foreach (var creator in linkRepository.GetActiveOfClients(clientId, DateTime.UtcNow).GroupBy(l => l.CreatedBy))
    {
      revoked.AddRange((await Unallowed(creator.Key, [.. creator]).ConfigureAwait(false)).Select(l => l.Id));
    }

    if (revoked.Count > 0)
    {
      await linkRepository.Revoke(revoked).ConfigureAwait(false);
    }
  }

  public async Task<bool> KeepIfAllowed(ShareLink link)
  {
    if (link.CreatedByAdmin || (await Unallowed(link.CreatedBy, [link]).ConfigureAwait(false)).Count == 0)
    {
      return true;
    }

    await linkRepository.Revoke([link.Id]).ConfigureAwait(false);
    return false;
  }

  // returns the links of the creator, which contain an existing record the creator may not share anymore.
  private async Task<IList<ShareLink>> Unallowed(string creator, IList<ShareLink> links)
  {
    List<Guid>? groups;
    try
    {
      // the cache may not be cleared yet, when the groups of the client changed.
      groups = await authClient.GetGroups(creator, cached: false).ConfigureAwait(false);
    }
    catch (HttpRequestException)
    {
      return [];
    }

    if (groups == null)
    {
      return [];
    }

    var existing = recordRepository.GetExistingIds(links.SelectMany(l => l.RecordIds).Distinct()).ToHashSet();
    var allowed = ruleRepository.FilterShareable(existing, groups).ToHashSet();
    return [.. links.Where(l => l.RecordIds.Any(id => existing.Contains(id) && !allowed.Contains(id)))];
  }
}

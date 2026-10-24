using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.API.Services;

public interface IAuthorizationClient
{

  /// <summary>
  /// Get groups for one app client.
  /// </summary>
  /// <param name="clientId">the id of client to load the groups for.</param>
  /// <returns>List of group ids.</returns>
  public Task<List<Guid>> GetGroups(string clientId);
}

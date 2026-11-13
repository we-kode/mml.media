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
  /// <param name="cached">False, to load the current groups from the identity service instead of the cached groups.</param>
  /// <returns>List of group ids, an empty list for unknown clients or null, if the identity service answered with an error.</returns>
  public Task<List<Guid>?> GetGroups(string clientId, bool cached = true);
}

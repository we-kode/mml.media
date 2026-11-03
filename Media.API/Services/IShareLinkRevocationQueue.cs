namespace Media.API.Services;

/// <summary>
/// Revokes the share links of clients in the background, which contain records their creator may not share anymore.
/// </summary>
public interface IShareLinkRevocationQueue
{
  /// <summary>
  /// Requests to check all links of clients. Requests made while a check is pending are combined into it.
  /// </summary>
  void Enqueue();
}

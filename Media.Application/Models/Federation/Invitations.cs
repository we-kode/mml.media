using System.Collections.Generic;

namespace Media.Application.Models.Federation;

/// <summary>
/// A list of open invitations.
/// </summary>
public class Invitations
{
  /// <summary>
  /// The total count of open invitations.
  /// </summary>
  public int TotalCount { get; set; }

  /// <summary>
  /// One chunk of loaded invitations.
  /// </summary>
  public IList<Invitation> Items { get; set; } = [];
}

using System;
using System.Collections.Generic;

namespace Media.API.Contracts;

/// <summary>
/// Request to synchronize records manually.
/// </summary>
public class SyncRequest
{
  /// <summary>
  /// Ids of the records.
  /// </summary>
  public IList<Guid> RecordIds { get; set; } = [];

  /// <summary>
  /// Folders whose records are synchronized. Records excluded from the synchronization with a partner are skipped.
  /// </summary>
  public IList<RecordFolder> Folders { get; set; } = [];

  /// <summary>
  /// The partners to synchronize with or null for all linked partners.
  /// </summary>
  public IList<Guid>? PeerIds { get; set; }
}

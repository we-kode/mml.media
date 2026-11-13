using Media.Application.Types;
using System;

namespace Media.Application.Models.Federation;

/// <summary>
/// A pending operation against a partner instance.
/// </summary>
public class FederationJob
{
  /// <summary>
  /// Id of the operation.
  /// </summary>
  public Guid JobId { get; set; }

  /// <summary>
  /// Id of the partner.
  /// </summary>
  public Guid PeerId { get; set; }

  /// <summary>
  /// The type of the operation.
  /// </summary>
  public FederationJobType Type { get; set; }

  /// <summary>
  /// The id of the related local record if available.
  /// </summary>
  public Guid? RecordId { get; set; }

  /// <summary>
  /// The id of the related transaction if available.
  /// </summary>
  public Guid? TransactionId { get; set; }

  /// <summary>
  /// Number of failed attempts.
  /// </summary>
  public int Attempts { get; set; }

  /// <summary>
  /// The audit log entry, which shows the state of the operation, if available.
  /// </summary>
  public Guid? AuditEntryId { get; set; }
}

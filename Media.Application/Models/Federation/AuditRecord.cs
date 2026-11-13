using System;

namespace Media.Application.Models.Federation;

/// <summary>
/// A record affected by an audit log entry.
/// </summary>
/// <param name="RecordId">Id of the record.</param>
/// <param name="Title">Current title of the record.</param>
public record AuditRecord(Guid RecordId, string Title);

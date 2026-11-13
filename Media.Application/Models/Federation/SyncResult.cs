namespace Media.Application.Models.Federation;

/// <summary>
/// Result of a manual synchronization.
/// </summary>
/// <param name="Queued">Number of queued transfers to partners.</param>
/// <param name="Skipped">Number of records not queued for any partner, because they are not owned, synchronized already or match no rule.</param>
public record SyncResult(int Queued, int Skipped);

using Media.Application.Types;

namespace Media.Application.Models.Federation;

/// <summary>
/// One column the audit log is sorted by.
/// </summary>
/// <param name="Field">The column.</param>
/// <param name="Descending">Sorts the column descending if true.</param>
public record AuditSort(AuditSortField Field, bool Descending);

using System;
using System.Collections.Generic;

namespace Media.API.Contracts;

/// <summary>
/// Result of the check, which of the requested records can be shared.
/// </summary>
/// <param name="Shareable">Number of the records the requester is allowed to share.</param>
/// <param name="Total">Number of the requested records, including the records of requested folders.</param>
/// <param name="Limit">Largest number of records in one link.</param>
/// <param name="ShareableRecords">Ids of the explicitly requested records the requester is allowed to share.</param>
public record ShareCheckResponse(int Shareable, int Total, int Limit, IList<Guid> ShareableRecords);

namespace Media.API.Contracts;

/// <summary>
/// Result of the creation of a share link.
/// </summary>
/// <param name="Token">Secret token of the link. The download url is /s/download/{token}.</param>
/// <param name="Shared">Number of the records in the link.</param>
/// <param name="Skipped">Number of the requested records, which the requester must not share.</param>
public record ShareResponse(string Token, int Shared, int Skipped);

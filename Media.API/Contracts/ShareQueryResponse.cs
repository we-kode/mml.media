namespace Media.API.Contracts;

/// <summary>
/// The short token of a shared search.
/// </summary>
/// <param name="Token">Token to be used in the link.</param>
public record ShareQueryResponse(string Token);

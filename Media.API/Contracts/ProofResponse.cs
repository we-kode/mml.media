namespace Media.API.Contracts;

/// <summary>
/// The signed random value of a partner instance.
/// </summary>
/// <param name="Assertion">The assertion containing the value as claim "nonce".</param>
public record ProofResponse(string Assertion);

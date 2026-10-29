using System.ComponentModel.DataAnnotations;

namespace Media.API.Contracts;

/// <summary>
/// Random value a partner instance asks this instance to sign.
/// </summary>
public class ProofRequest
{
  /// <summary>
  /// The random value.
  /// </summary>
  [Required]
  public string Nonce { get; set; } = string.Empty;
}

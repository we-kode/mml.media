using System.Net;
using System.Net.Http;

namespace Media.Application.Models.Federation;

/// <summary>
/// Result of an executed operation against a partner.
/// </summary>
/// <param name="Done">Whether the operation is finished and removed from the queue.</param>
/// <param name="Error">The error, if the operation failed.</param>
public record FederationJobResult(bool Done, string? Error)
{
  /// <summary>
  /// The operation succeeded.
  /// </summary>
  public static FederationJobResult Success { get; } = new(true, null);

  /// <summary>
  /// The operation failed temporarily and is retried.
  /// </summary>
  /// <param name="error">The error.</param>
  /// <returns><see cref="FederationJobResult"/></returns>
  public static FederationJobResult Retry(string error) => new(false, error);

  /// <summary>
  /// The operation failed permanently.
  /// </summary>
  /// <param name="error">The error.</param>
  /// <returns><see cref="FederationJobResult"/></returns>
  public static FederationJobResult Failed(string error) => new(true, error);

  /// <summary>
  /// Evaluates the response of a partner. Server errors, timeouts and throttling are retried.
  /// </summary>
  /// <param name="response">The response.</param>
  /// <param name="notFoundIsSuccess">Treats a missing resource as success.</param>
  /// <returns><see cref="FederationJobResult"/></returns>
  public static FederationJobResult FromResponse(HttpResponseMessage response, bool notFoundIsSuccess = false)
  {
    if (response.IsSuccessStatusCode || (notFoundIsSuccess && response.StatusCode == HttpStatusCode.NotFound))
    {
      return Success;
    }

    var status = (int)response.StatusCode;
    if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
    {
      return Failed("The data is too large for the upload limit of the partner (413).");
    }

    var transient = status >= 500 || response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;
    return transient ? Retry($"Partner returned {status}.") : Failed($"Partner returned {status}.");
  }
}

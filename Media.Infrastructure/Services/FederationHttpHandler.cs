using Media.Application.Models.Federation;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Infrastructure.Services;

/// <summary>
/// Creates the http handler used for requests to partner instances.
/// </summary>
public static class FederationHttpHandler
{
  /// <summary>
  /// Creates a handler which only connects to public addresses unless private networks are allowed and never follows redirects.
  /// </summary>
  /// <param name="options">The federation configuration.</param>
  /// <param name="acceptAnyCertificate">Accepts invalid server certificates. Only intended for development.</param>
  /// <returns><see cref="SocketsHttpHandler"/></returns>
  public static SocketsHttpHandler Create(FederationOptions options, bool acceptAnyCertificate)
  {
    var handler = new SocketsHttpHandler
    {
      AllowAutoRedirect = false,
      UseCookies = false,
      ConnectTimeout = TimeSpan.FromSeconds(15),
      PooledConnectionLifetime = TimeSpan.FromMinutes(5),
      ConnectCallback = (context, cancellationToken) => Connect(context, options.AllowPrivateNetworks, cancellationToken),
    };

    if (acceptAnyCertificate)
    {
      handler.SslOptions = new SslClientAuthenticationOptions
      {
        RemoteCertificateValidationCallback = (_, _, _, _) => true,
      };
    }

    return handler;
  }

  /// <summary>
  /// Validates that the url can be used to reach a partner.
  /// </summary>
  /// <param name="url">The url to validate.</param>
  /// <returns>The parsed url.</returns>
  /// <exception cref="FederationException">If the url is not an absolute https url.</exception>
  public static Uri ValidateUrl(string? url)
  {
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
      || uri.Scheme != Uri.UriSchemeHttps
      || !string.IsNullOrEmpty(uri.UserInfo)
      || !string.IsNullOrEmpty(uri.Fragment)
      || !string.IsNullOrEmpty(uri.Query))
    {
      throw new FederationException(FederationErrors.InvalidUrl);
    }

    return uri;
  }

  /// <summary>
  /// Returns whether the address is in a private, loopback, link local or otherwise not public network.
  /// </summary>
  /// <param name="address">The address to check.</param>
  /// <returns>True, if the address is not public.</returns>
  public static bool IsPrivate(IPAddress address)
  {
    if (address.IsIPv4MappedToIPv6)
    {
      address = address.MapToIPv4();
    }

    if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
    {
      return true;
    }

    if (address.AddressFamily == AddressFamily.InterNetworkV6)
    {
      return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal || address.IsIPv6Multicast;
    }

    var bytes = address.GetAddressBytes();
    return bytes[0] switch
    {
      0 or 10 or 127 => true,
      100 => bytes[1] >= 64 && bytes[1] <= 127,
      169 => bytes[1] == 254,
      172 => bytes[1] >= 16 && bytes[1] <= 31,
      192 => bytes[1] == 168 || (bytes[1] == 0 && bytes[2] == 0),
      >= 224 => true,
      _ => false,
    };
  }

  private static async ValueTask<System.IO.Stream> Connect(SocketsHttpConnectionContext context, bool allowPrivateNetworks, CancellationToken cancellationToken)
  {
    var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken).ConfigureAwait(false);
    var allowed = addresses.Where(a => allowPrivateNetworks || !IsPrivate(a)).ToArray();
    if (allowed.Length == 0)
    {
      throw new HttpRequestException($"Connections to '{context.DnsEndPoint.Host}' are not allowed.");
    }

    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
    try
    {
      await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
      return new NetworkStream(socket, ownsSocket: true);
    }
    catch
    {
      socket.Dispose();
      throw;
    }
  }
}

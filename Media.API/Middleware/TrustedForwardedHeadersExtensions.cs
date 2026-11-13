using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Media.API.Middleware;

public static class TrustedForwardedHeadersExtensions
{
  /// <summary>
  /// Configuration key of the networks whose proxies are trusted to send forwarded headers.
  /// </summary>
  public const string KNOWN_NETWORKS = "ForwardedHeaders:KnownNetworks";

  /// <summary>
  /// Networks trusted when <see cref="KNOWN_NETWORKS"/> is not configured.
  /// </summary>
  public static readonly string[] DefaultKnownNetworks = ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"];

  /// <summary>
  /// Accepts the X-Forwarded-For and X-Forwarded-Proto headers from proxies in the configured networks or, by default, in private networks.
  /// </summary>
  /// <exception cref="System.FormatException">A configured network is not a valid CIDR notation.</exception>
  public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
  {
    var configured = configuration.GetSection(KNOWN_NETWORKS).Get<string[]>();
    var networks = (configured is { Length: > 0 } ? configured : DefaultKnownNetworks).Select(System.Net.IPNetwork.Parse).ToList();
    return services.Configure<ForwardedHeadersOptions>(options =>
    {
      options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
      options.KnownProxies.Clear();
      options.KnownIPNetworks.Clear();
      networks.ForEach(options.KnownIPNetworks.Add);
    });
  }
}

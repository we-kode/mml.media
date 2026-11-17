using Microsoft.Extensions.Configuration;
using System;

namespace Media.API.Extensions;

/// <summary>
/// Reads values of the configuration.
/// </summary>
public static class ConfigurationExtensions
{
  /// <summary>
  /// Creates the AMQP connection string from the <c>MessageBus</c> section. User, password and virtual host are escaped,
  /// so they may contain reserved URI characters.
  /// </summary>
  /// <param name="configuration">The configuration.</param>
  /// <returns>The AMQP connection string.</returns>
  /// <exception cref="ArgumentNullException">Host, user or password is not configured.</exception>
  public static string GetMessageBusConnectionString(this IConfiguration configuration)
  {
    var host = configuration["MessageBus:Host"] ?? throw new ArgumentNullException("MessageBus:Host");
    var virtualHost = configuration["MessageBus:VirtualHost"];
    var user = configuration["MessageBus:User"] ?? throw new ArgumentNullException("MessageBus:User");
    var password = configuration["MessageBus:Password"] ?? throw new ArgumentNullException("MessageBus:Password");

    var connection = $"amqp://{Uri.EscapeDataString(user)}:{Uri.EscapeDataString(password)}@{host}";
    return string.IsNullOrEmpty(virtualHost) ? connection : $"{connection}/{Uri.EscapeDataString(virtualHost)}";
  }
}

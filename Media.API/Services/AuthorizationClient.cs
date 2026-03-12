using Messages.Events;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Rebus.Handlers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Media.API.Services;

public class AuthorizationClient(HttpClient httpClient, IMemoryCache cache, IConfiguration configuration) : IAuthorizationClient, IHandleMessages<ClientStateUpdated>
{
  private string? accessToken;
  private DateTime expiresAt = DateTime.UtcNow;
  private const string CACHE_KEY = "MML_CLIENT_GROUPS_";

  public async Task<List<Guid>> GetGroups(string clientId)
  {
    if (cache.TryGetValue($"{CACHE_KEY}{clientId}", out List<Guid>? cached))
    {
      return cached ?? [];
    }

    var token = await GetAccessTokenAsync();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var result = await httpClient.GetAsync($"api/v1.0/identity/internal/clientinfo/{clientId}/groups");
    if (!result.IsSuccessStatusCode)
    {
      return [];
    }

    var groups = await result.Content.ReadFromJsonAsync<List<Guid>>();
    if (groups != null)
    {
      var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
      cache.Set($"{CACHE_KEY}{clientId}", groups, cacheEntryOptions);
    }

    return groups ?? [];
  }

  private async Task<string> GetAccessTokenAsync()
  {
    if (accessToken != null && expiresAt > DateTime.UtcNow.AddSeconds(30))
    {
      return accessToken;
    }

    var clientId = configuration["ApiClient:ClientId"] ?? throw new ArgumentNullException("ApiClient:ClientId", "An api client id must be provided.");
    var clientSecret = configuration["ApiClient:ClientSecret"] ?? throw new ArgumentNullException("ApiClient:ClientId", "An api client id must be provided.");

    var requestData = new Dictionary<string, string>
    {
        { "grant_type", "client_credentials" },
        { "client_id", clientId},
        { "client_secret",  clientSecret},
        { "scope", "wekode.mml.identity.internal" }
    };

    var response = await httpClient.PostAsync("api/v1.0/identity/connect/token", new FormUrlEncodedContent(requestData));

    if (!response.IsSuccessStatusCode)
    {
      return string.Empty;
    }

    var content = await response.Content.ReadAsStringAsync();
    using var json = JsonDocument.Parse(content);
    accessToken = json.RootElement.GetProperty("access_token").GetString();
    int expiresInSeconds = json.RootElement.GetProperty("expires_in").GetInt32();
    expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
    return accessToken ?? string.Empty;
  }

  public async Task Handle(ClientStateUpdated message)
  {
    cache.Remove($"{CACHE_KEY}{message.ClientId}");
  }
}

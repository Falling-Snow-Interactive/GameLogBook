using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameLogBook.Models.Igdb;
using GameLogBook.Models.Configuration;
using GameLogBook.Models.Library;
using Microsoft.Extensions.Options;

namespace GameLogBook.Services;

public class IgdbService(HttpClient httpClient, IOptions<IgdbSettings> options)
{
    private const string TwitchTokenUrl = "https://id.twitch.tv/oauth2/token";
    private const string IgdbGamesUrl = "https://api.igdb.com/v4/games";

    private readonly IgdbSettings settings = options.Value;

    private string? accessToken;
    private DateTimeOffset accessTokenExpiresAt;

    public async Task<IReadOnlyList<Game>> SearchGamesAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return [];
        }

        string token = await GetAccessTokenAsync();

        using HttpRequestMessage request = new(HttpMethod.Post, IgdbGamesUrl);
        request.Headers.Add("Client-ID", settings.ClientId);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(BuildSearchQuery(searchText), Encoding.UTF8, "text/plain");

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        List<Igdb>? results = JsonSerializer.Deserialize<List<Igdb>>(json, JsonOptions);

        return results?
               .Select(igdbGame => igdbGame.ToLibraryGame())
               .ToList() ?? [];
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(accessToken) && DateTimeOffset.UtcNow < accessTokenExpiresAt)
        {
            return accessToken;
        }

        string url = $"{TwitchTokenUrl}?client_id={Uri.EscapeDataString(settings.ClientId)}&client_secret={Uri.EscapeDataString(settings.ClientSecret)}&grant_type=client_credentials";

        using HttpResponseMessage response = await httpClient.PostAsync(url, null);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        TwitchTokenResponse? tokenResponse = JsonSerializer.Deserialize<TwitchTokenResponse>(json, JsonOptions);

        if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Twitch did not return a valid IGDB access token.");
        }

        accessToken = tokenResponse.AccessToken;
        accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, tokenResponse.ExpiresIn - 60));
        
        return accessToken;
    }

    private static string BuildSearchQuery(string searchText)
    {
        string escapedSearchText = searchText.Replace("\\", "\\\\").Replace("\"", "\\\"");

        return $"""
               search "{escapedSearchText}";
               fields name, first_release_date, summary, cover.url, cover.width, cover.game, involved_companies.developer, involved_companies.publisher, involved_companies.company.name;
               limit 10;
               """;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
                                                                {
                                                                    PropertyNameCaseInsensitive = true,
                                                                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                                                                };

    private sealed class TwitchTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }
}
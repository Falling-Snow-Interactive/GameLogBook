using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameLogBook.Models.Configuration;
using GameLogBook.Models.Games;
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
        List<IgdbGameDto>? results = JsonSerializer.Deserialize<List<IgdbGameDto>>(json, JsonOptions);

        return results?
               .Select(ToGame)
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
               fields name, first_release_date, summary, cover.url;
               limit 10;
               """;
    }

    private static Game ToGame(IgdbGameDto dto)
    {
        return new Game
               {
                   IgdbId = dto.Id,
                   Title = dto.Name ?? string.Empty,
                   ReleaseDate = ConvertUnixTimeToDateOnly(dto.FirstReleaseDate),
                   Summary = dto.Summary,
                   CoverUrl = NormalizeCoverUrl(dto.Cover?.Url)
               };
    }

    private static DateOnly? ConvertUnixTimeToDateOnly(long? unixTime)
    {
        if (unixTime is null)
        {
            return null;
        }

        return DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unixTime.Value).DateTime);
    }

    private static string? NormalizeCoverUrl(string? coverUrl)
    {
        if (string.IsNullOrWhiteSpace(coverUrl))
        {
            return null;
        }

        return coverUrl.StartsWith("//")
                   ? $"https:{coverUrl}"
                   : coverUrl;
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

    private sealed class IgdbGameDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public long? FirstReleaseDate { get; set; }
        public string? Summary { get; set; }
        public IgdbCoverDto? Cover { get; set; }
    }

    private sealed class IgdbCoverDto
    {
        public string? Url { get; set; }
    }
}
using craftersmine.SteamGridDBNet;
using Microsoft.EntityFrameworkCore;
using VGL.Data;
using VGL.Models.Configuration;

namespace VGL.Services;

public class SteamGridDbClientProvider(GameLogBookDbContext dbContext)
{
    private SteamGridDb? client;
    private string? clientApiKey;

    public async Task<bool> IsConfiguredAsync()
    {
        string apiKey = await GetApiKeyAsync();
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    public async Task<SteamGridDb> GetClientAsync()
    {
        string apiKey = (await GetApiKeyAsync()).Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("SteamGridDB API key is not configured.");
        }

        if (client is null || !string.Equals(clientApiKey, apiKey, StringComparison.Ordinal))
        {
            client = new SteamGridDb(apiKey);
            clientApiKey = apiKey;
        }

        return client;
    }

    private async Task<string> GetApiKeyAsync()
    {
        return await dbContext.AppSettings
                              .AsNoTracking()
                              .Where(settings => settings.ID == AppSettings.SingletonId)
                              .Select(settings => settings.SteamGridDbApiKey)
                              .FirstOrDefaultAsync()
               ?? string.Empty;
    }
}

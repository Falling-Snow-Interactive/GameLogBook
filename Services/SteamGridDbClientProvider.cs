using craftersmine.SteamGridDBNet;
using Microsoft.Extensions.Options;
using VGL.Models.Configuration;

namespace VGL.Services;

public class SteamGridDbClientProvider(IOptions<SteamGridDbSettings> options)
{
    private readonly SteamGridDbSettings settings = options.Value;
    private SteamGridDb? client;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.ApiKey);

    public SteamGridDb GetClient()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("SteamGridDB API key is not configured.");
        }

        client ??= new SteamGridDb(settings.ApiKey.Trim());
        return client;
    }
}

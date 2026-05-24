using GameLogBook.Models.Configuration;
using IGDB;
using Microsoft.Extensions.Options;

namespace GameLogBook.Services;

public class IGDBClientProvider(IOptions<IgdbSettings> options)
{
    private readonly IgdbSettings settings = options.Value;
    private IGDBClient? client;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(settings.ClientId)
        && !string.IsNullOrWhiteSpace(settings.ClientSecret);

    public IGDBClient GetClient()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("IGDB credentials are not configured.");
        }

        client ??= IGDBClient.CreateWithDefaults(settings.ClientId, settings.ClientSecret);
        return client;
    }
}

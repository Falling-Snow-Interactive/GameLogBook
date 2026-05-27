using craftersmine.SteamGridDBNet;
using craftersmine.SteamGridDBNet.Exceptions;

namespace VGL.Services;

public class SteamGridDbArtworkService(SteamGridDbClientProvider clientProvider)
{
    private const int ResultLimit = 50;

    public bool IsConfigured => clientProvider.IsConfigured;

    public async Task<IReadOnlyList<SteamGridDbGameSearchResult>> SearchGamesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        SteamGridDbGame[]? games;
        try
        {
            games = await clientProvider.GetClient().SearchForGamesAsync(searchTerm.Trim());
        }
        catch (SteamGridDbNotFoundException)
        {
            return [];
        }

        return (games ?? [])
               .Where(game => game.Id > 0 && !string.IsNullOrWhiteSpace(game.Name))
               .Select(game => new SteamGridDbGameSearchResult(
                           game.Id,
                           game.Name,
                           game.ReleaseDate,
                           game.Verified))
               .OrderByNameRelevance(searchTerm, game => game.Name)
               .Take(ResultLimit)
               .ToList();
    }

    public async Task<IReadOnlyList<SteamGridDbImageSearchResult>> SearchImagesAsync(
        int gameId,
        SteamGridDbImageType imageType)
    {
        if (gameId <= 0)
        {
            return [];
        }

        SteamGridDbObject[] images;
        try
        {
            SteamGridDb client = clientProvider.GetClient();
            images = imageType switch
            {
                SteamGridDbImageType.Cover => await client.GetGridsByGameIdAsync(
                    gameId,
                    nsfw: false,
                    humorous: false,
                    epilepsy: false,
                    page: 0,
                    tags: SteamGridDbTags.None,
                    styles: SteamGridDbStyles.AllGrids,
                    dimensions: SteamGridDbDimensions.AllGrids,
                    formats: SupportedImageFormats,
                    types: SteamGridDbTypes.Static,
                    limit: ResultLimit),
                SteamGridDbImageType.Hero => await client.GetHeroesByGameIdAsync(
                    gameId,
                    nsfw: false,
                    humorous: false,
                    epilepsy: false,
                    page: 0,
                    tags: SteamGridDbTags.None,
                    styles: SteamGridDbStyles.AllHeroes,
                    dimensions: SteamGridDbDimensions.AllHeroes,
                    formats: SupportedImageFormats,
                    types: SteamGridDbTypes.Static,
                    limit: ResultLimit),
                SteamGridDbImageType.Logo => await client.GetLogosByGameIdAsync(
                    gameId,
                    nsfw: false,
                    humorous: false,
                    epilepsy: false,
                    page: 0,
                    tags: SteamGridDbTags.None,
                    styles: SteamGridDbStyles.AllLogos,
                    formats: SteamGridDbFormats.Png | SteamGridDbFormats.Webp,
                    types: SteamGridDbTypes.Static,
                    limit: ResultLimit),
                SteamGridDbImageType.Icon => await client.GetIconsByGameIdAsync(
                    gameId,
                    nsfw: false,
                    humorous: false,
                    epilepsy: false,
                    page: 0,
                    tags: SteamGridDbTags.None,
                    styles: SteamGridDbStyles.AllIcons,
                    formats: SteamGridDbFormats.Png,
                    types: SteamGridDbTypes.Static,
                    limit: ResultLimit),
                _ => []
            };
        }
        catch (SteamGridDbNotFoundException)
        {
            return [];
        }

        return images
               .Where(image => !string.IsNullOrWhiteSpace(image.FullImageUrl))
               .Select(image => new SteamGridDbImageSearchResult(
                           image.Id,
                           image.Width,
                           image.Height,
                           image.Style.ToString(),
                           image.Format.ToString(),
                           image.FullImageUrl,
                           string.IsNullOrWhiteSpace(image.ThumbnailImageUrl)
                               ? image.FullImageUrl
                               : image.ThumbnailImageUrl))
               .ToList();
    }

    private const SteamGridDbFormats SupportedImageFormats =
        SteamGridDbFormats.Png | SteamGridDbFormats.Jpeg | SteamGridDbFormats.Webp;
}

public enum SteamGridDbImageType
{
    Cover,
    Logo,
    Hero,
    Icon
}

public sealed record SteamGridDbGameSearchResult(
    int Id,
    string Name,
    DateTime? ReleaseDate,
    bool Verified);

public sealed record SteamGridDbImageSearchResult(
    int Id,
    int Width,
    int Height,
    string Style,
    string Format,
    string FullImageUrl,
    string ThumbnailImageUrl);

internal static class SteamGridDbSearchOrderingExtensions
{
    public static IOrderedEnumerable<T> OrderByNameRelevance<T>(
        this IEnumerable<T> source,
        string searchTerm,
        Func<T, string> getName)
    {
        string trimmedSearchTerm = searchTerm.Trim();

        return source
               .OrderBy(item => GetNameRank(getName(item), trimmedSearchTerm))
               .ThenBy(item => getName(item), StringComparer.OrdinalIgnoreCase);
    }

    private static int GetNameRank(string name, string searchTerm)
    {
        if (string.Equals(name, searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ? 2 : 3;
    }
}

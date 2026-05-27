using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using VGL.Services;

namespace VGL.Components.Popups;

public partial class SteamGridDbImageSearchPopup
{
    private const int MinSearchLength = 2;

    private string searchInput = string.Empty;
    private string? selectedGameId;
    private string? errorMessage;
    private bool isSearchingGames;
    private bool isLoadingImages;
    private bool hasSearchedGames;
    private bool hasLoadedImages;

    private List<SteamGridDbGameSearchResult> gameResults = [];
    private List<SteamGridDbImageSearchResult> imageResults = [];

    [Inject]
    private SteamGridDbArtworkService SteamGridDbArtworkService { get; set; } = null!;

    [CascadingParameter]
    private PopupInstance? Popup { get; set; }

    [Parameter]
    public string InitialSearchTerm { get; set; } = string.Empty;

    [Parameter]
    public SteamGridDbImageType ImageType { get; set; }

    private string ImageTypeLabel => ImageType.ToString();

    private string PopupTitle => $"Search SteamGridDB {ImageTypeLabel}";

    private string PopupDescription => "Choose a SteamGridDB entry, then pick artwork to download into this field.";

    protected override async Task OnInitializedAsync()
    {
        searchInput = InitialSearchTerm ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(searchInput))
        {
            await SearchGames();
        }
    }

    private void HandleSearchInputChanged(ChangeEventArgs args)
    {
        searchInput = args.Value?.ToString() ?? string.Empty;
        errorMessage = null;
    }

    private async Task HandleSearchKeyDown(KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or "NumpadEnter")
        {
            await SearchGames();
        }
    }

    private async Task SearchGames()
    {
        errorMessage = null;
        gameResults = [];
        imageResults = [];
        selectedGameId = null;
        hasSearchedGames = false;
        hasLoadedImages = false;

        if (!SteamGridDbArtworkService.IsConfigured)
        {
            return;
        }

        string trimmedSearch = searchInput.Trim();
        if (trimmedSearch.Length < MinSearchLength)
        {
            errorMessage = "Enter at least 2 characters to search SteamGridDB.";
            return;
        }

        isSearchingGames = true;

        try
        {
            gameResults = (await SteamGridDbArtworkService.SearchGamesAsync(trimmedSearch)).ToList();
            hasSearchedGames = true;

            SteamGridDbGameSearchResult? firstGame = gameResults.FirstOrDefault();
            if (firstGame is not null)
            {
                selectedGameId = firstGame.Id.ToString();
                await LoadImages(firstGame.Id);
            }
        }
        catch (Exception exception)
        {
            errorMessage = GetFriendlyErrorMessage(exception);
        }
        finally
        {
            isSearchingGames = false;
        }
    }

    private async Task HandleSelectedGameChanged(ChangeEventArgs args)
    {
        selectedGameId = args.Value?.ToString();
        imageResults = [];
        hasLoadedImages = false;
        errorMessage = null;

        if (int.TryParse(selectedGameId, out int gameId))
        {
            await LoadImages(gameId);
        }
    }

    private async Task LoadImages(int gameId)
    {
        isLoadingImages = true;
        hasLoadedImages = false;
        imageResults = [];

        try
        {
            imageResults = (await SteamGridDbArtworkService.SearchImagesAsync(gameId, ImageType)).ToList();
            hasLoadedImages = true;
        }
        catch (Exception exception)
        {
            errorMessage = GetFriendlyErrorMessage(exception);
        }
        finally
        {
            isLoadingImages = false;
        }
    }

    private async Task SelectImage(SteamGridDbImageSearchResult image)
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync(image.FullImageUrl);
        }
    }

    private async Task HandleClose()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync();
        }
    }

    private static string GetGameLabel(SteamGridDbGameSearchResult game)
    {
        string label = game.Name;

        if (game.ReleaseDate is { Year: > 1 } releaseDate)
        {
            label += $" ({releaseDate.Year})";
        }

        if (game.Verified)
        {
            label += " - verified";
        }

        return label;
    }

    private static string GetImageSummary(SteamGridDbImageSearchResult image)
    {
        string dimensions = image.Width > 0 && image.Height > 0
                                ? $"{image.Width}x{image.Height}"
                                : "Image";

        return $"{dimensions} - {image.Style}";
    }

    private static string GetFriendlyErrorMessage(Exception exception)
    {
        return exception.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains("API key", StringComparison.OrdinalIgnoreCase)
                   ? "SteamGridDB rejected the configured API key."
                   : $"SteamGridDB search failed: {exception.Message}";
    }
}

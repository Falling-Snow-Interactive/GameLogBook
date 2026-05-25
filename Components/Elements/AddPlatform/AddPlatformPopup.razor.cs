using GameLogBook.Components.Elements.IGDBSearch;
using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using GameLogBook.Services;
using IGDB;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using IgdbGame = IGDB.Models.Game;
using Platform = IGDB.Models.Platform;
using PlatformModel = GameLogBook.Models.Platforms.Platform;

namespace GameLogBook.Components.Elements.AddPlatform;

public partial class AddPlatformPopup : ComponentBase
{
    [Inject]
    protected IGDBClientProvider IgdbClientProvider { get; set; } = null!;

    [Inject]
    protected LocalImageService LocalImageService { get; set; } = null!;

    [CascadingParameter]
    private PopupInstance? Popup { get; set; }

    private PlatformModel? previousInitialPlatform;
    private string platformName = string.Empty;
    private string abbreviation = string.Empty;
    private string platformImagePath = string.Empty;
    private string platformImageUrl = string.Empty;
    private string? platformPreviewSource;
    private IBrowserFile? uploadedPlatformImage;
    private bool isSaving;
    private DateOnly? releaseDate;
    private long? igdbId;
    private HashSet<int> selectedGameIds = [];
    private HashSet<int> companyIds = [];

    private string? searchErrorMessage;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<PlatformModel> OnPlatformSelected { get; set; }

    [Parameter]
    public IReadOnlyList<Game> Games { get; set; } = [];

    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    [Parameter]
    public PlatformModel? InitialPlatform { get; set; }

    private string PopupTitle => InitialPlatform is null ? "Add Platform" : "Edit Platform";

    private string SaveButtonText => InitialPlatform is null ? "Add Platform" : "Save Changes";

    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(previousInitialPlatform, InitialPlatform))
        {
            return;
        }

        previousInitialPlatform = InitialPlatform;

        if (InitialPlatform is null)
        {
            ResetForm();
            return;
        }

        await LoadPlatform(InitialPlatform);
    }

    private async Task HandlePlatformSelected(IgdbSearchPlatformResult result)
    {
        PlatformModel platform = result.Platform;
        igdbId = platform.IgdbId;
        platformName = platform.Name;
        abbreviation = platform.Abbreviation;
        platformImagePath = platform.ImagePath ?? string.Empty;
        platformImageUrl = platform.PendingImageUrl ?? string.Empty;
        uploadedPlatformImage = null;
        platformPreviewSource = !string.IsNullOrWhiteSpace(platformImageUrl)
                                    ? platformImageUrl
                                    : await LocalImageService.GetImageSourceAsync(platformImagePath);
        releaseDate = platform.ReleaseDate;
        searchErrorMessage = null;
        // selectedCompanyIds = GetMatchingLocalManufacturerIds(result.ManufacturerNames);
        // selectedCompanyId = string.Empty;

        await PopulateSelectedGames(platform.IgdbId);
    }

    private async Task HandleSavePlatform()
    {
        var name = platformName.Trim();

        isSaving = true;
        searchErrorMessage = null;

        string? imagePath;
        try
        {
            imagePath = await ResolvePlatformImagePath();
        }
        catch (Exception exception)
        {
            searchErrorMessage = exception.Message;
            isSaving = false;
            return;
        }

        int[] manufacturerIds = companyIds
                                .OrderBy(companyId => companyId)
                                .ToArray();
        int[] gameIds = selectedGameIds
                        .OrderBy(gameId => gameId)
                        .ToArray();

        PlatformModel platform = new(name)
                                 {
                                     ID = InitialPlatform?.ID ?? 0,

                                     IgdbId = igdbId,
                                     Abbreviation = abbreviation,
                                     ReleaseDate = releaseDate,
                                     ManufacturerIds = manufacturerIds,

                                     ImagePath = imagePath,
                                 };

        if (Popup is not null)
        {
            await Popup.CloseAsync(platform);
        }
        else
        {
            await OnPlatformSelected.InvokeAsync(platform);
        }

        isSaving = false;
    }

    private async Task HandleClose()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync();
            return;
        }

        await OnClose.InvokeAsync();
    }

    private Task HandleCompanyIdsChanged(HashSet<int> updatedCompanyIds)
    {
        companyIds = updatedCompanyIds;
        return Task.CompletedTask;
    }

    private void ToggleGameSelection(int gameId, ChangeEventArgs args)
    {
        if (args.Value is true)
        {
            selectedGameIds.Add(gameId);
            return;
        }

        selectedGameIds.Remove(gameId);
    }

    private async Task PopulateSelectedGames(long? platformIgdbId)
    {
        selectedGameIds.Clear();

        if (platformIgdbId <= 0 || Games.Count == 0 || !IgdbClientProvider.IsConfigured)
        {
            return;
        }

        long?[] localIgdbGameIds = Games
                                   .Where(game => game.IgdbId > 0)
                                   .Select(game => game.IgdbId)
                                   .ToArray();

        if (localIgdbGameIds.Length == 0)
        {
            return;
        }

        try
        {
            string localIgdbGameIdsFilter = string.Join(",", localIgdbGameIds);

            IgdbGame[] igdbGames = await IgdbClientProvider
                                         .GetClient()
                                         .QueryAsync<IgdbGame>(
                                                               IGDBClient.Endpoints.Games,
                                                               query: $"""
                                                                       fields id;
                                                                       where id = ({localIgdbGameIdsFilter})
                                                                             & platforms = {platformIgdbId};
                                                                       limit {localIgdbGameIds.Length};
                                                                       """);

            HashSet<long> matchedGameIds = igdbGames
                                           .Where(game => game.Id.HasValue)
                                           .Select(game => game.Id!.Value)
                                           .ToHashSet();

            selectedGameIds = Games
                              .Where(game => game.IgdbId.HasValue && matchedGameIds.Contains(game.IgdbId.Value))
                              .Select(game => game.ID)
                              .ToHashSet();
        }
        catch (Exception exception)
        {
            searchErrorMessage = IsAuthenticationFailure(exception)
                                     ? "Search failed: IGDB credentials were rejected. Check the configured client ID and client secret."
                                     : $"Could not load linked games: {exception.Message}";
        }
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        return exception.Message.Contains("id.twitch.tv/oauth2/token", StringComparison.OrdinalIgnoreCase);
    }

    private async Task LoadPlatform(PlatformModel platform)
    {
        igdbId = platform.IgdbId;
        platformName = platform.Name;
        abbreviation = platform.Abbreviation;
        platformImagePath = platform.ImagePath ?? string.Empty;
        platformImageUrl = platform.PendingImageUrl ?? string.Empty;
        uploadedPlatformImage = null;
        platformPreviewSource = !string.IsNullOrWhiteSpace(platformImageUrl)
                                    ? platformImageUrl
                                    : await LocalImageService.GetImageSourceAsync(platformImagePath);
        releaseDate = platform.ReleaseDate;
        // selectedGameIds = (platform.GameIds ?? []).ToHashSet();
        companyIds = (platform.ManufacturerIds ?? []).ToHashSet();
        searchErrorMessage = null;
    }

    private void ResetForm()
    {
        platformName = string.Empty;
        abbreviation = string.Empty;
        platformImagePath = string.Empty;
        platformImageUrl = string.Empty;
        platformPreviewSource = null;
        uploadedPlatformImage = null;
        isSaving = false;
        releaseDate = null;
        igdbId = 0;
        selectedGameIds = [];
        companyIds = [];
        searchErrorMessage = null;
    }

    private async Task HandlePlatformImageUrlChanged(ChangeEventArgs args)
    {
        platformImageUrl = args.Value?.ToString() ?? string.Empty;
        uploadedPlatformImage = null;
        searchErrorMessage = null;
        platformPreviewSource = !string.IsNullOrWhiteSpace(platformImageUrl)
                                    ? platformImageUrl
                                    : await LocalImageService.GetImageSourceAsync(platformImagePath);
    }

    private async Task HandlePlatformImageFileSelected(InputFileChangeEventArgs args)
    {
        uploadedPlatformImage = args.File;
        platformImageUrl = string.Empty;
        searchErrorMessage = null;

        try
        {
            platformPreviewSource = await LocalImageService.GetUploadPreviewSourceAsync(uploadedPlatformImage);
        }
        catch (Exception exception)
        {
            uploadedPlatformImage = null;
            platformPreviewSource = await LocalImageService.GetImageSourceAsync(platformImagePath);
            searchErrorMessage = exception.Message;
        }
    }

    private void RemovePlatformImage()
    {
        platformImagePath = string.Empty;
        platformImageUrl = string.Empty;
        platformPreviewSource = null;
        uploadedPlatformImage = null;
        searchErrorMessage = null;
    }

    private async Task<string?> ResolvePlatformImagePath()
    {
        if (uploadedPlatformImage is not null)
        {
            return await LocalImageService.SaveUploadedImageAsync(uploadedPlatformImage, "platforms");
        }

        if (!string.IsNullOrWhiteSpace(platformImageUrl))
        {
            return await LocalImageService.DownloadImageAsync(platformImageUrl, "platforms");
        }

        return string.IsNullOrWhiteSpace(platformImagePath) ? null : platformImagePath;
    }
}

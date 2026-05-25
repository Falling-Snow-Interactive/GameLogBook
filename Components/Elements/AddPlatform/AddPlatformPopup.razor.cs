using GameLogBook.Components.Elements.IGDBSearch;
using GameLogBook.Components.Elements.ImageField;
using GameLogBook.Models;
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
    private string? abbreviation = string.Empty;
    private string? summary = string.Empty;
    
    private bool isSaving;
    private DateOnly? releaseDate;
    private long? igdbId;
    
    private HashSet<int> selectedGameIds = [];
    private HashSet<int> companyIds = [];

    private string? searchErrorMessage;
    private string? imageErrorMessage;
    
    private ImageFieldWidget? coverImageField;
    private ImageFieldWidget? heroImageField;
    private ImageFieldWidget? logoImageField;
    private ImageFieldWidget? iconImageField;
    
    private string coverImagePath = string.Empty;
    private string heroImagePath = string.Empty;
    private string logoImagePath = string.Empty;
    private string iconImagePath = string.Empty;
    
    private string coverImageUrl = string.Empty;
    private string heroImageUrl = string.Empty;
    private string logoImageUrl = string.Empty;
    private string iconImageUrl = string.Empty;

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

    private async Task HandleIGDBSearchPlatformSelected(IgdbSearchPlatformResult result)
    {
        PlatformModel platform = result.Platform;
        igdbId = platform.IgdbId;
        platformName = platform.Name;
        abbreviation = platform.Abbreviation;
        releaseDate = platform.ReleaseDate;
        summary = platform.Summary;
        
        searchErrorMessage = null;
        
        coverImagePath = result.Platform.Cover?.ImagePath ?? string.Empty;
        coverImageUrl = result.Platform.Cover?.PendingImageUrl ?? string.Empty;
        
        heroImagePath = result.Platform.Hero?.ImagePath ?? string.Empty;
        heroImageUrl = result.Platform.Hero?.PendingImageUrl ?? string.Empty;

        logoImagePath = result.Platform.Logo?.ImagePath ?? string.Empty;
        logoImageUrl = result.Platform.Logo?.PendingImageUrl ?? string.Empty;

        await PopulateSelectedGames(platform.IgdbId);
    }

    private async Task HandleSavePlatform()
    {
        var name = platformName.Trim();
        var summary = this.summary.Trim();

        isSaving = true;
        searchErrorMessage = null;
        imageErrorMessage = null;

        int[] manufacturerIds = companyIds
                                .OrderBy(companyId => companyId)
                                .ToArray();
        
        string? coverPath;
        try
        {
            coverPath = coverImageField is null ? ResolveExistingCoverImagePath() : await coverImageField.CommitAsync();
        }
        catch (Exception exception)
        {
            imageErrorMessage = exception.Message;
            isSaving = false;
            return;
        }
        
        string? heroPath;
        try
        {
            heroPath = heroImageField is null ? ResolveExistingHeroImagePath() : await heroImageField.CommitAsync();
        }
        catch (Exception exception)
        {
            imageErrorMessage = exception.Message;
            isSaving = false;
            return;
        }
        
        string? logoPath;
        try
        {
            logoPath = logoImageField is null ? ResolveExistingLogoImagePath() : await logoImageField.CommitAsync();
        }
        catch (Exception exception)
        {
            imageErrorMessage = exception.Message;
            isSaving = false;
            return;
        }
        
        string? iconPath;
        try
        {
            iconPath = iconImageField is null ? ResolveExistingIconImagePath() : await iconImageField.CommitAsync();
        }
        catch (Exception exception)
        {
            imageErrorMessage = exception.Message;
            isSaving = false;
            return;
        }

        PlatformModel platform = new(name)
                                 {
                                     ID = InitialPlatform?.ID ?? 0,

                                     IgdbId = igdbId,
                                     Abbreviation = abbreviation,
                                     ReleaseDate = releaseDate,
                                     Summary = summary,
                                     
                                     ManufacturerIds = manufacturerIds,

                                     #region Images
                                     Cover = string.IsNullOrWhiteSpace(coverPath)
                                                 ? null
                                                 : new ImageRef
                                                   {
                                                       ImagePath = coverPath
                                                   },
                        
                                     Hero = string.IsNullOrWhiteSpace(heroPath) 
                                                ? null 
                                                : new ImageRef
                                                  {
                                                      ImagePath = heroPath,
                                                  },
                        
                                     Logo = string.IsNullOrWhiteSpace(logoPath) 
                                                ? null 
                                                : new ImageRef
                                                  {
                                                      ImagePath = logoPath,
                                                  },
                                     
                                     Icon = string.IsNullOrWhiteSpace(iconPath) 
                                                ? null 
                                                : new ImageRef
                                                  {
                                                      ImagePath = iconPath,
                                                  },
                                     #endregion
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

    private void TogglePlatformSelection(int gameId, ChangeEventArgs args)
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

    private Task LoadPlatform(PlatformModel platform)
    {
        igdbId = platform.IgdbId;
        platformName = platform.Name;
        abbreviation = platform.Abbreviation;
        summary = platform.Summary;
        
        releaseDate = platform.ReleaseDate;
        companyIds = (platform.ManufacturerIds ?? []).ToHashSet();
        searchErrorMessage = null;
        
        coverImagePath = platform.Cover?.ImagePath ?? string.Empty;
        coverImageUrl = platform.Cover?.PendingImageUrl ?? string.Empty;
        
        heroImagePath = platform.Hero?.ImagePath ?? string.Empty;
        heroImageUrl = platform.Hero?.PendingImageUrl ?? string.Empty;

        logoImagePath = platform.Logo?.ImagePath ?? string.Empty;
        logoImageUrl = platform.Logo?.PendingImageUrl ?? string.Empty;

        iconImagePath = platform.Icon?.ImagePath ?? string.Empty;
        iconImageUrl = platform.Icon?.PendingImageUrl ?? string.Empty;
        
        return Task.CompletedTask;
    }

    private void ResetForm()
    {
        platformName = string.Empty;
        abbreviation = string.Empty;
        summary = string.Empty;
        
        isSaving = false;
        releaseDate = null;
        igdbId = 0;
        selectedGameIds = [];
        companyIds = [];
        searchErrorMessage = null;
    }
    
    private string? ResolveExistingCoverImagePath()
    {
        return string.IsNullOrWhiteSpace(coverImagePath) ? null : coverImagePath;
    }
    
    private string? ResolveExistingHeroImagePath()
    {
        return string.IsNullOrWhiteSpace(heroImagePath) ? null : heroImagePath;
    }
    
    private string? ResolveExistingLogoImagePath()
    {
        return string.IsNullOrWhiteSpace(logoImagePath) ? null : logoImagePath;
    }
    
    private string? ResolveExistingIconImagePath()
    {
        return string.IsNullOrWhiteSpace(iconImagePath) ? null : iconImagePath;
    }
}

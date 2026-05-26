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
    // Inject
    [Inject]
    protected IGDBClientProvider IgdbClientProvider { get; set; } = null!;

    [Inject]
    protected LocalImageService LocalImageService { get; set; } = null!;

    // Parameters
    [CascadingParameter]
    private PopupInstance? Popup { get; set; }
    
    [Parameter]
    public IReadOnlyList<Game> Games { get; set; } = [];

    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    [Parameter]
    public PlatformModel? InitialPlatform { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<PlatformModel> OnPlatformSelected { get; set; }
    
    [Parameter]
    public Func<Company, Task<Company?>>? OnCompanyAdded { get; set; }
    
    private string PopupTitle => InitialPlatform is null ? "Add Platform" : "Edit Platform";
    private string SaveButtonText => InitialPlatform is null ? "Add Platform" : "Save Changes";

    // Previous
    private PlatformModel? previousInitialPlatform;
    private IReadOnlyList<Company>? previousCompanies;
    
    // Input
    private bool isSaving;
    
    // Information
    private string name = string.Empty;
    private string? nameShort = string.Empty;
    private string? summary = string.Empty;
    
    private DateOnly? releaseDate;
    
    private HashSet<int> selectedGameIds = [];
    private List<int> companyIDs = [];
    
    // Images
    private ImageFieldWidget? coverImageField;
    private ImageFieldWidget? heroImageField;
    private ImageFieldWidget? logoImageField;
    private ImageFieldWidget? iconImageField;
    
    private string coverPath = string.Empty;
    private string heroPath = string.Empty;
    private string logoPath = string.Empty;
    private string iconImagePath = string.Empty;
    
    private string coverUrl = string.Empty;
    private string heroUrl = string.Empty;
    private string logoUrl = string.Empty;
    private string iconImageUrl = string.Empty;
    
    // IGDB
    private long? igdbId;

    // Errors
    private string? searchErrorMessage;
    private string? imageErrorMessage;

    // Caches
    private List<Company>? companyCache;

    private string developerSearchText = string.Empty;
    
    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(previousCompanies, Companies))
        {
            previousCompanies = Companies;
            companyCache = Companies
                           .OrderBy(company => company.Name)
                           .ToList();
        }
        
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
        name = platform.Name;
        nameShort = platform.Abbreviation;
        releaseDate = platform.ReleaseDate;
        summary = platform.Summary;
        
        searchErrorMessage = null;
        
        coverPath = result.Platform.Cover?.Path ?? string.Empty;
        coverUrl = result.Platform.Cover?.PendingUrl ?? string.Empty;
        
        heroPath = result.Platform.Hero?.Path ?? string.Empty;
        heroUrl = result.Platform.Hero?.PendingUrl ?? string.Empty;

        logoPath = result.Platform.Logo?.Path ?? string.Empty;
        logoUrl = result.Platform.Logo?.PendingUrl ?? string.Empty;
        
        iconImagePath = result.Platform.Icon?.Path ?? string.Empty;
        iconImageUrl = result.Platform.Icon?.PendingUrl ?? string.Empty;

        await PopulateSelectedGames(platform.IgdbId);
    }

    private async Task HandleSavePlatform()
    {
        isSaving = true;
        searchErrorMessage = null;
        imageErrorMessage = null;
        
        string name = this.name.Trim();
        string? platformSummary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();

        int[] manufacturerIds = companyIDs
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
                                     Abbreviation = nameShort,
                                     ReleaseDate = releaseDate,
                                     Summary = platformSummary,
                                     
                                     ManufacturerIds = manufacturerIds,

                                     #region Images
                                     Cover = string.IsNullOrWhiteSpace(coverPath)
                                                 ? null
                                                 : new ImageRef
                                                   {
                                                       Path = coverPath
                                                   },
                        
                                     Hero = string.IsNullOrWhiteSpace(heroPath) 
                                                ? null 
                                                : new ImageRef
                                                  {
                                                      Path = heroPath,
                                                  },
                        
                                     Logo = string.IsNullOrWhiteSpace(logoPath) 
                                                ? null 
                                                : new ImageRef
                                                  {
                                                      Path = logoPath,
                                                  },
                                     
                                     Icon = string.IsNullOrWhiteSpace(iconPath) 
                                                ? null 
                                                : new ImageRef
                                                  {
                                                      Path = iconPath,
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

    private Task HandleCompanyIDsChanged(List<int> updatedCompanyIds)
    {
        companyIDs = updatedCompanyIds;
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
                                   .Where(game => game.IGDB > 0)
                                   .Select(game => game.IGDB)
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
                              .Where(game => game.IGDB.HasValue && matchedGameIds.Contains(game.IGDB.Value))
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
        // Information
        name = platform.Name;
        summary = platform.Summary;
        nameShort = platform.Abbreviation;
        releaseDate = platform.ReleaseDate;
        
        // Companies
        companyIDs = (platform.ManufacturerIds ?? []).ToList();
        
        // Images
        coverPath = platform.Cover?.Path ?? string.Empty;
        coverUrl = platform.Cover?.PendingUrl ?? string.Empty;
        
        heroPath = platform.Hero?.Path ?? string.Empty;
        heroUrl = platform.Hero?.PendingUrl ?? string.Empty;

        logoPath = platform.Logo?.Path ?? string.Empty;
        logoUrl = platform.Logo?.PendingUrl ?? string.Empty;

        iconImagePath = platform.Icon?.Path ?? string.Empty;
        iconImageUrl = platform.Icon?.PendingUrl ?? string.Empty;
        
        // IGDB
        igdbId = platform.IgdbId;
        
        // Search
        developerSearchText = string.Empty;
        searchErrorMessage = null;
        
        return Task.CompletedTask;
    }

    private void ResetForm()
    {
        isSaving = false;
        
        // Information
        name = string.Empty;
        nameShort = string.Empty;
        summary = string.Empty;
        releaseDate = null;
        
        // Games
        selectedGameIds = [];
        
        // Companies
        companyIDs = [];
        
        // Images
        coverPath = string.Empty;
        coverUrl = string.Empty;
        
        heroPath = string.Empty;
        heroUrl = string.Empty;

        logoPath = string.Empty;
        logoUrl = string.Empty;

        iconImagePath = string.Empty;
        iconImageUrl = string.Empty;
        
        // IGDB
        igdbId = 0;
        
        // Errors
        imageErrorMessage = null;
        searchErrorMessage = null;
    }
    
    private string? ResolveExistingCoverImagePath()
    {
        return string.IsNullOrWhiteSpace(coverPath) ? null : coverPath;
    }
    
    private string? ResolveExistingHeroImagePath()
    {
        return string.IsNullOrWhiteSpace(heroPath) ? null : heroPath;
    }
    
    private string? ResolveExistingLogoImagePath()
    {
        return string.IsNullOrWhiteSpace(logoPath) ? null : logoPath;
    }
    
    private string? ResolveExistingIconImagePath()
    {
        return string.IsNullOrWhiteSpace(iconImagePath) ? null : iconImagePath;
    }

    private List<int> ResolveLocalCompanyIDs(IEnumerable<int> companyIDs)
    {
        return companyIDs
               .Where(companyID => companyCache != null 
                                   && companyCache.Any(company => company.ID == companyID))
               .Distinct()
               .Order()
               .ToList();
    }

    private void AddOrUpdateAvailableCompany(Company company)
    {
        if (companyCache == null)
        {
            return;
        }

        int existingIndex = companyCache.FindIndex(existingCompany => existingCompany.ID == company.ID);

        if (existingIndex >= 0)
        {
            companyCache[existingIndex] = company;
        }
        else
        {
            companyCache.Add(company);
        }

        companyCache = companyCache
                       .OrderBy(existingCompany => existingCompany.Name, StringComparer.CurrentCultureIgnoreCase)
                       .ToList();
    }

    private async Task<Company?> HandleCompanyAdded(Company company)
    {
        if (OnCompanyAdded is null)
        {
            return null;
        }

        Company? savedCompany = await OnCompanyAdded.Invoke(company);

        if (savedCompany is not null)
        {
            AddOrUpdateAvailableCompany(savedCompany);
            await InvokeAsync(StateHasChanged);
        }

        return savedCompany;
    }
}

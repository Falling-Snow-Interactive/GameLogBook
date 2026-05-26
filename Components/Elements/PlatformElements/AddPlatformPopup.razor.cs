using IGDB;
using Microsoft.AspNetCore.Components;
using VGL.Components.Elements.IGDBSearch;
using VGL.Components.Elements.ImageField;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Services;
using IGDBGame = IGDB.Models.Game;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Components.Elements.PlatformElements;

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
    public Platform? InitialPlatform { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Platform> OnPlatformSelected { get; set; }
    
    [Parameter]
    public Func<Company, Task<Company?>>? OnCompanyAdded { get; set; }
    
    private string PopupTitle => InitialPlatform is null ? "Add Platform" : "Edit Platform";
    private string SaveButtonText => InitialPlatform is null ? "Add Platform" : "Save Changes";

    // ----- Runtime -----
    
    // Previous
    private Platform? previousInitialPlatform;
    private IReadOnlyList<Company>? previousCompanies;
    
    // Controls
    private bool isSaving;
    
    // Input
    // Information
    private string name = string.Empty;
    private string? nameShort = string.Empty;
    private string? summary = string.Empty;
    
    private DateOnly? releaseDate;
    
    private HashSet<int> selectedGameIDs = [];
    private List<int> companyIDs = [];
    
    // Images
    private ImageFieldWidget? coverField;
    private ImageFieldWidget? heroField;
    private ImageFieldWidget? logoField;
    private ImageFieldWidget? iconField;

    private ImageRef? cover;
    private ImageRef? hero;
    private ImageRef? logo;
    private ImageRef? icon;
    
    // APIs
    private long? igdb;
    
    // Searches
    private string developerSearchText = string.Empty;

    // Errors
    private string? searchErrorMessage;
    private string? imageErrorMessage;

    // Caches
    private List<Company>? companyCache;

    public AddPlatformPopup()
    {
        icon = null;
        logo = null;
    }

    #region Overrides
    
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
            Reset();
            return;
        }

        await Load(InitialPlatform);
    }
    
    #endregion
    
    #region IGDB

    private async Task IGDBSearch_PlatformSelected(IGDBSearchPlatformResult result)
    {
        Platform platform = result.Platform;
        
        // Information
        name = platform.Name;
        nameShort = platform.ShortName;
        releaseDate = platform.ReleaseDate;
        summary = platform.Summary;
        
        // Images
        cover = null;
        hero = null;
        logo = null;
        icon = null;
        
        // APIs
        igdb = platform.IGDB;
        
        // Errors
        searchErrorMessage = null;

        await PopulateSelectedGames(platform.IGDB);
    }
    
    #endregion
    
    #region Input Handlers

    private async Task SaveButton_Clicked()
    {
        await Save();
    }

    private async Task CloseButton_Clicked()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync();
            return;
        }

        await OnClose.InvokeAsync();
    }
    
    #endregion

    #region Change Handlers

    #endregion
    
    #region Platform Selection

    private void TogglePlatformSelection(int gameId, ChangeEventArgs args)
    {
        if (args.Value is true)
        {
            selectedGameIDs.Add(gameId);
            return;
        }

        selectedGameIDs.Remove(gameId);
    }
    
    #endregion
    
    #region Games

    private async Task PopulateSelectedGames(long? platformIgdbId)
    {
        selectedGameIDs.Clear();

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

            IGDBGame[] igdbGames = await IgdbClientProvider
                                         .GetClient()
                                         .QueryAsync<IGDBGame>(
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

            selectedGameIDs = Games
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
    
    #endregion
    
    #region Authentication

    private static bool IsAuthenticationFailure(Exception exception)
    {
        return exception.Message.Contains("id.twitch.tv/oauth2/token", StringComparison.OrdinalIgnoreCase);
    }
    
    #endregion
    
    #region Controls

    private async Task Save()
    {
        isSaving = true;
        searchErrorMessage = null;
        imageErrorMessage = null;

        try
        {
            ImageRef? coverRef = await coverField?.CommitAsync()!;
            ImageRef? heroRef = await heroField?.CommitAsync()!;
            ImageRef? logoRef = await logoField?.CommitAsync()!;
            ImageRef? iconRef = await iconField?.CommitAsync()!;
            
            Platform platform = new(name.Trim())
                                {
                                    // Database
                                    ID = InitialPlatform?.ID ?? 0,

                                    // Information
                                    ShortName = nameShort,
                                    ReleaseDate = releaseDate,
                                    Summary = summary?.Trim(),
                                    ManufacturerIds = companyIDs.OrderBy(companyId => companyId).ToArray(),
                                         
                                    // Images
                                    Cover = coverRef,
                                    Hero = heroRef,
                                    Logo = logoRef,
                                    Icon = iconRef,
                                         
                                    // IGDB
                                    IGDB = igdb,
                                };
            
            if (Popup is not null)
            {
                await Popup.CloseAsync(platform);
            }
            else
            {
                await OnPlatformSelected.InvokeAsync(platform);
            }
        }
        catch (Exception e)
        {
            imageErrorMessage = e.Message;
        }

        isSaving = false;
    }

    private Task Load(Platform platform)
    {
        // Information
        name = platform.Name;
        summary = platform.Summary;
        nameShort = platform.ShortName;
        releaseDate = platform.ReleaseDate;
        
        // Companies
        companyIDs = (platform.ManufacturerIds ?? []).ToList();
        
        // Images
        cover = platform.Cover;
        hero = platform.Hero;
        logo = platform.Logo;
        icon = platform.Icon;
        
        // IGDB
        igdb = platform.IGDB;
        
        // Search
        developerSearchText = string.Empty;
        searchErrorMessage = null;
        
        return Task.CompletedTask;
    }

    private void Reset()
    {
        // Saving
        isSaving = false;
        
        // Information
        name = string.Empty;
        nameShort = string.Empty;
        summary = string.Empty;
        releaseDate = null;
        
        // Games
        selectedGameIDs = [];
        
        // Companies
        companyIDs = [];
        
        // Images
        cover = null;
        hero = null;
        logo = null;
        icon = null;
        
        // IGDB
        igdb = -1;
        
        // Errors
        imageErrorMessage = null;
        searchErrorMessage = null;
    }
    
    #endregion

    #region Company Control
    
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
    
    #endregion
}

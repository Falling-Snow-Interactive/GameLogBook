using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using VGL.Components.Elements.ImageField;
using VGL.Models;
using VGL.Models.Games;
using VGL.Models.Games.Company;
using VGL.Models.Games.Platforms;
using VGL.Services;
using Company = VGL.Models.Companies.Company;
using Game = VGL.Models.Games.Game;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Components.Popups;

public partial class AddGamePopup
{
    // ----- Parameters -----
    
    [CascadingParameter]
    private PopupInstance? Popup { get; set; }
    
    [Parameter]
    public Game? InitialGame { get; set; }
    
    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    [Parameter]
    public IReadOnlyList<Platform> Platforms { get; set; } = [];
    
    [Parameter]
    public EventCallback<Game> OnGameSelected { get; set; }
    
    [Parameter]
    public Func<Company, Task<Company?>>? OnCompanyAdded { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }
    
    
    // ----- Runtime -----
    
    // Previous
    private Game? previousInitialGame;
    private IReadOnlyList<Company>? previousCompanies;

    private List<Company> availableCompanies = [];
    private List<int> selectedDeveloperIDs = [];
    private List<int> selectedPublisherIDs = [];
    private List<GamePlatformRelation> selectedPlatformOwnerships = [];
    
    private string developerSearchText = string.Empty;
    private string publisherSearchText = string.Empty;

    public string name = string.Empty;
    private GameType type;
    private int rating;
    private DateOnly? releaseDate;
    private string summary = string.Empty;
    
    private ImageFieldWidget? coverField;
    private ImageFieldWidget? heroField;
    private ImageFieldWidget? logoField;
    private ImageFieldWidget? iconField;

    private ImageRef? cover;
    private ImageRef? hero;
    private ImageRef? logo;
    private ImageRef? icon;
    
    private long? igdb;
    
    private string? imageErrorMessage;
    private bool isSaving;

    private string PopupTitle => InitialGame is null ? "Add Game" : "Edit Game";

    private string SaveButtonText => InitialGame is null ? "Add to Library" : "Save Changes";

    #region Overrides
    
    protected override Task OnParametersSetAsync()
    {
        if (!ReferenceEquals(previousCompanies, Companies))
        {
            previousCompanies = Companies;
            availableCompanies = Companies
                                 .OrderBy(company => company.Name)
                                 .ToList();
        }

        if (ReferenceEquals(previousInitialGame, InitialGame))
        {
            return Task.CompletedTask;
        }

        previousInitialGame = InitialGame;

        if (InitialGame is null)
        {
            Reset();
            return Task.CompletedTask;
        }

        Load(InitialGame);
        return Task.CompletedTask;
    }
    
    #endregion
    
    #region Game Selected
    
    private void HandleGameSelected(Game game)
    {
        Load(game);
    }
    
    #endregion

    #region Input Handlers
    
    private async Task HandleSaveButtonClick()
    {
        await Save();
    }

    private async Task HandleCloseButtonClick()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync();
            return;
        }

        await OnClose.InvokeAsync();
    }
    
    #endregion
    
    #region Form Controls

    private async Task Save()
    {
        isSaving = true;
        imageErrorMessage = null;
        
        try
        {
            ImageRef? coverRef = await coverField?.CommitAsync()!;
            ImageRef? heroRef = await heroField?.CommitAsync()!;
            ImageRef? logoRef = await logoField?.CommitAsync()!;
            ImageRef? iconRef = await iconField?.CommitAsync()!;
            
            Game game = new(name.Trim())
                        {
                            // Database
                            ID = InitialGame?.ID ?? 0,
                        
                            // Information
                            Name = name.Trim(),
                            GameType = type,
                            Rating = Math.Clamp(rating, 0, Game.MaxRating),
                            ReleaseDate = releaseDate,
                            Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                        
                            // Images
                            Cover = CopyImageRef(coverRef),
                            Hero = CopyImageRef(heroRef),
                            Logo = CopyImageRef(logoRef),
                            Icon = CopyImageRef(iconRef),
                            
                            // IGDB
                            IGDB = igdb,
                        };
            
            game.AddCompaniesByID(GameCompanyRole.Developer, selectedDeveloperIDs);
            game.AddCompaniesByID(GameCompanyRole.Publisher, selectedPublisherIDs);
            game.GamePlatforms = NormalizePlatformOwnerships(selectedPlatformOwnerships);
            
            if (Popup is not null)
            {
                await Popup.CloseAsync(game);
            }
            else
            {
                await OnGameSelected.InvokeAsync(game);
            }

            isSaving = false;
        }
        catch (Exception exception)
        {
            imageErrorMessage = exception.Message;
            isSaving = false;
        }
    }

    private void Load(Game game)
    {
        // Information
        name = string.IsNullOrWhiteSpace(game.Name) ? name : game.Name;
        summary = string.IsNullOrWhiteSpace(game.Summary) ? summary : game.Summary;
        type = game.GameType != GameType.None ? game.GameType : type;
        rating = game.Rating > rating ? game.Rating : rating;
        releaseDate = game.ReleaseDate ?? releaseDate;
        
        // Companies
        selectedDeveloperIDs = game.GetDeveloperIDs().Order().Distinct().ToList();
        selectedPublisherIDs = game.GetPublisherIDs().Order().Distinct().ToList();
        selectedPlatformOwnerships = NormalizePlatformOwnerships(game.GamePlatforms);

        // Images
        cover = CheckOverrideImage(cover, game.Cover);
        hero = CheckOverrideImage(hero, game.Hero);
        logo = CheckOverrideImage(logo, game.Logo);
        icon = CheckOverrideImage(icon, game.Icon);
        
        // IGDB
        igdb = game.IGDB;
        
        // Search
        developerSearchText = string.Empty;
        publisherSearchText = string.Empty;
        
        // Errors
        imageErrorMessage = null;
    }
    
    private void Reset()
    {
        isSaving = false;

        // Information
        name = string.Empty;     
        summary = string.Empty;
        type = GameType.None;
        rating = 0;
        releaseDate = null;
        
        // Developers
        selectedDeveloperIDs = [];
        selectedPublisherIDs = [];     
        selectedPlatformOwnerships = [];
        
        // Search
        developerSearchText = string.Empty;
        publisherSearchText = string.Empty;
        
        // APIs
        igdb = 0;
        
        // Images
        cover = null;
        hero = null;
        logo = null;
        icon = null;
        
        // Errors
        imageErrorMessage = null;
    }
    
    #endregion

    #region Company Added
    
    private async Task<Company?> HandleCompanyAdded(Company company)
    {
        if (OnCompanyAdded is null)
        {
            return null;
        }

        Company? saved = await OnCompanyAdded.Invoke(company);

        if (saved is not null)
        {
            AddOrUpdateAvailableCompany(saved);
            await InvokeAsync(StateHasChanged);
        }

        return saved;
    }

    private void AddOrUpdateAvailableCompany(Company company)
    {
        int existingIndex = availableCompanies.FindIndex(existingCompany => existingCompany.ID == company.ID);

        if (existingIndex >= 0)
        {
            availableCompanies[existingIndex] = company;
        }
        else
        {
            availableCompanies.Add(company);
        }

        availableCompanies = availableCompanies
                             .OrderBy(existingCompany => existingCompany.Name, StringComparer.OrdinalIgnoreCase)
                             .ToList();
    }

    private static List<GamePlatformRelation> NormalizePlatformOwnerships(IEnumerable<GamePlatformRelation> ownerships)
    {
        return ownerships
               .Where(ownership => ownership.PlatformID > 0
                                   && ownership.Ownership != OwnershipType.None)
               .GroupBy(ownership => new
                                     {
                                         ownership.PlatformID,
                                         ownership.Ownership
                                     })
               .Select(group => group.First())
               .OrderBy(ownership => ownership.PlatformID)
               .ThenBy(ownership => ownership.Ownership)
               .ToList();
    }
    
    #endregion
    
    private ImageRef? CheckOverrideImage(ImageRef? imageRef, ImageRef? newRef)
    {
        if (newRef == null)
        {
            return imageRef;
        }
        
        if (imageRef == null)
        {
            return newRef;
        }

        return newRef.IsValid() ? newRef : imageRef;
    }

    private static ImageRef? CopyImageRef(ImageRef? image)
    {
        return string.IsNullOrWhiteSpace(image?.Path)
                   ? null
                   : new ImageRef
                     {
                         Path = image.Path.Trim()
                     };
    }
}

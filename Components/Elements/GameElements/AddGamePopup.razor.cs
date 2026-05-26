using GameLogBook.Components.Elements.ImageField;
using GameLogBook.Models;
using GameLogBook.Models.Games;
using GameLogBook.Services;
using Microsoft.AspNetCore.Components;
using Company = GameLogBook.Models.Companies.Company;
using Game = GameLogBook.Models.Games.Game;

namespace GameLogBook.Components.Elements.GameElements;

public partial class AddGamePopup
{
    private Game? previousInitialGame;
    private IReadOnlyList<Company>? previousCompanies;

    [CascadingParameter]
    private PopupInstance? Popup { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Game> OnGameSelected { get; set; }

    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    [Parameter]
    public Func<Company, Task<Company?>>? OnCompanyAdded { get; set; }

    [Parameter]
    public Game? InitialGame { get; set; }

    private List<Company> availableCompanies = [];
    private List<int> selectedDeveloperIDs = [];
    private List<int> selectedPublisherIDs = [];
    
    private string developerSearchText = string.Empty;
    private string publisherSearchText = string.Empty;

    private string name = string.Empty;
    private long? igdb;
    private DateOnly? releaseDate;
    
    private ImageFieldWidget? coverImageField;
    private ImageFieldWidget? heroImageField;
    private ImageFieldWidget? logoImageField;
    private ImageFieldWidget? iconImageField;

    private ImageRef? cover;
    private ImageRef? hero;
    private ImageRef? logo;
    private ImageRef? icon;
    
    private string coverImagePath = string.Empty;
    private string heroImagePath = string.Empty;
    private string logoImagePath = string.Empty;
    private string iconImagePath = string.Empty;
    
    private string coverImageUrl = string.Empty;
    private string heroImageUrl = string.Empty;
    private string logoImageUrl = string.Empty;
    private string iconImageUrl = string.Empty;
    
    private string? imageErrorMessage;
    private bool isSaving;
    private string summary = string.Empty;
    private GameType type;

    private string PopupTitle => InitialGame is null ? "Add Game" : "Edit Game";

    private string SaveButtonText => InitialGame is null ? "Add to Library" : "Save Changes";

    protected override async Task OnParametersSetAsync()
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
            return;
        }

        previousInitialGame = InitialGame;

        if (InitialGame is null)
        {
            ResetForm();
            return;
        }

        await LoadGame(InitialGame);
    }

    private async Task HandleGameSelected(Game game)
    {
        await LoadGame(game);
    }

    private async Task HandleSaveGame()
    {
        isSaving = true;
        imageErrorMessage = null;

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
        
        Game game = new()
                    {
                        ID = InitialGame?.ID ?? 0,
                        
                        IGDB = igdb,
                        
                        Name = name.Trim(),
                        GameType = type,
                        ReleaseDate = releaseDate,
                        
                        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                        
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
        
        game.AddCompaniesByID(GameCompanyRole.Developer, selectedDeveloperIDs);
        game.AddCompaniesByID(GameCompanyRole.Publisher, selectedPublisherIDs);

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

    private async Task HandleClose()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync();
            return;
        }

        await OnClose.InvokeAsync();
    }

    private async Task LoadGame(Game game)
    {
        // Information
        name = game.Name;
        summary = game.Summary ?? string.Empty;
        type = game.GameType;
        releaseDate = game.ReleaseDate;
        
        // Companies
        selectedDeveloperIDs = ResolveLocalCompanyIDs(game.GetDeveloperIDs());
        selectedPublisherIDs = ResolveLocalCompanyIDs(game.GetPublisherIDs());

        // Images
        cover = game.Cover;
        hero = game.Hero;
        logo = game.Logo;
        icon = game.Icon;
        
        coverImagePath = game.Cover?.Path ?? string.Empty;
        coverImageUrl = game.Cover?.PendingUrl ?? string.Empty;
        
        heroImagePath = game.Hero?.Path ?? string.Empty;
        heroImageUrl = game.Hero?.PendingUrl ?? string.Empty;

        logoImagePath = game.Logo?.Path ?? string.Empty;
        logoImageUrl = game.Logo?.PendingUrl ?? string.Empty;
        
        iconImagePath = game.Icon?.Path ?? string.Empty;
        iconImageUrl = game.Icon?.PendingUrl ?? string.Empty;  
        
        // IGDB
        igdb = game.IGDB;
        
        // Search
        developerSearchText = string.Empty;
        publisherSearchText = string.Empty;
        
        // Errors
        imageErrorMessage = null;
    }

    private void ResetForm()
    {
        selectedDeveloperIDs = [];
        selectedPublisherIDs = [];
        developerSearchText = string.Empty;
        publisherSearchText = string.Empty;
        name = string.Empty;
        igdb = 0;
        releaseDate = null;
        
        coverImagePath = string.Empty;
        coverImageUrl = string.Empty;
        
        heroImagePath = string.Empty;
        heroImageUrl = string.Empty;

        logoImagePath = string.Empty;
        logoImageUrl = string.Empty;

        iconImagePath = string.Empty;
        iconImageUrl = string.Empty;
        
        imageErrorMessage = null;
        isSaving = false;
        summary = string.Empty;
        type = GameType.None;
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

    private List<int> ResolveLocalCompanyIDs(IEnumerable<int> companyIds)
    {
        return companyIds
               .Where(companyId => availableCompanies.Any(company => company.ID == companyId))
               .Distinct()
               .Order()
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
}

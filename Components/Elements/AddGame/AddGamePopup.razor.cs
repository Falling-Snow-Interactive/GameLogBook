using Microsoft.AspNetCore.Components;
using Company = GameLogBook.Models.Companies.Company;
using Cover = GameLogBook.Models.Games.Cover;
using Game = GameLogBook.Models.Games.Game;
using GameLogBook.Models.Games;

namespace GameLogBook.Components.Elements.AddGame;

public partial class AddGamePopup
{
    private Game? previousInitialGame;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Game> OnGameSelected { get; set; }

    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    [Parameter]
    public Game? InitialGame { get; set; }

    private List<int> selectedDeveloperCompanyIds = [];
    private List<int> selectedPublisherCompanyIds = [];
    private string developerSearchText = string.Empty;
    private string publisherSearchText = string.Empty;

    private string gameName = string.Empty;
    private long igdbId;
    private DateOnly? releaseDate;
    private string coverUrl = string.Empty;
    private string summary = string.Empty;

    private string PopupTitle => InitialGame is null ? "Add Game" : "Edit Game";

    private string SaveButtonText => InitialGame is null ? "Add to Library" : "Save Changes";

    protected override void OnParametersSet()
    {
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

        LoadGame(InitialGame);
    }

    private Task HandleGameSelected(Game game)
    {
        LoadGame(game);
        return Task.CompletedTask;
    }

    private async Task HandleSaveGame()
    {
        Game game = new()
                    {
                        Id = InitialGame?.Id ?? 0,
                        IgdbId = igdbId,
                        Name = gameName.Trim(),
                        ReleaseDate = releaseDate,
                        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                        Cover = string.IsNullOrWhiteSpace(coverUrl)
                                    ? null
                                    : new Cover
                                      {
                                          Url = coverUrl.Trim()
                                      },
                        Companies = BuildGameCompanies()
                    };

        await OnGameSelected.InvokeAsync(game);
    }

    private void LoadGame(Game game)
    {
        igdbId = game.IgdbId;
        gameName = game.Name;
        selectedDeveloperCompanyIds = ResolveLocalCompanyIds(game, GameCompanyRole.Developer);
        selectedPublisherCompanyIds = ResolveLocalCompanyIds(game, GameCompanyRole.Publisher);
        developerSearchText = string.Empty;
        publisherSearchText = string.Empty;
        releaseDate = game.ReleaseDate;
        coverUrl = game.Cover?.Url ?? string.Empty;
        summary = game.Summary ?? string.Empty;
    }

    private void ResetForm()
    {
        selectedDeveloperCompanyIds = [];
        selectedPublisherCompanyIds = [];
        developerSearchText = string.Empty;
        publisherSearchText = string.Empty;
        gameName = string.Empty;
        igdbId = 0;
        releaseDate = null;
        coverUrl = string.Empty;
        summary = string.Empty;
    }

    private void SelectDeveloper(Company company)
    {
        AddSelectedCompany(selectedDeveloperCompanyIds, company.Id);
        developerSearchText = string.Empty;
    }

    private void SelectPublisher(Company company)
    {
        AddSelectedCompany(selectedPublisherCompanyIds, company.Id);
        publisherSearchText = string.Empty;
    }

    private void RemoveDeveloper(int companyId)
    {
        selectedDeveloperCompanyIds.Remove(companyId);
    }

    private void RemovePublisher(int companyId)
    {
        selectedPublisherCompanyIds.Remove(companyId);
    }

    private List<GameCompany> BuildGameCompaniesForRole(IEnumerable<int> companyIds, GameCompanyRole role)
    {
        return Companies
               .Where(company => companyIds.Contains(company.Id))
               .Select(company => new GameCompany
                                  {
                                      Role = role,
                                      Company = company
                                  })
               .ToList();
    }

    private List<GameCompany> BuildGameCompanies()
    {
        return BuildGameCompaniesForRole(selectedDeveloperCompanyIds, GameCompanyRole.Developer)
               .Concat(BuildGameCompaniesForRole(selectedPublisherCompanyIds, GameCompanyRole.Publisher))
               .ToList();
    }

    private List<int> ResolveLocalCompanyIds(Game game, GameCompanyRole role)
    {
        return game.Companies
                   .Where(gameCompany => gameCompany.Role == role)
                   .Select(gameCompany => ResolveLocalCompany(gameCompany.Company))
                   .Where(company => company is not null)
                   .Select(company => company!.Id)
                   .Distinct()
                   .ToList();
    }

    private Company? ResolveLocalCompany(Company company)
    {
        if (company.Id > 0)
        {
            Company? byId = Companies.FirstOrDefault(localCompany => localCompany.Id == company.Id);
            if (byId is not null)
            {
                return byId;
            }
        }

        if (company.IgdbId.HasValue)
        {
            Company? byIgdbId = Companies.FirstOrDefault(localCompany => localCompany.IgdbId == company.IgdbId.Value);
            if (byIgdbId is not null)
            {
                return byIgdbId;
            }
        }

        return Companies.FirstOrDefault(localCompany =>
                                            string.Equals(localCompany.Name,
                                                          company.Name,
                                                          StringComparison.OrdinalIgnoreCase));
    }

    private IReadOnlyList<Company> DeveloperMatches => FilterCompanies(developerSearchText, selectedDeveloperCompanyIds);

    private IReadOnlyList<Company> PublisherMatches => FilterCompanies(publisherSearchText, selectedPublisherCompanyIds);

    private IReadOnlyList<Company> SelectedDeveloperCompanies => GetSelectedCompanies(selectedDeveloperCompanyIds);

    private IReadOnlyList<Company> SelectedPublisherCompanies => GetSelectedCompanies(selectedPublisherCompanyIds);

    private IReadOnlyList<Company> FilterCompanies(string searchText, IReadOnlyCollection<int> selectedIds)
    {
        string trimmedSearchText = searchText.Trim();

        return Companies
               .Where(company => !selectedIds.Contains(company.Id))
               .Where(company => string.IsNullOrWhiteSpace(trimmedSearchText)
                                  || company.Name.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase))
               .OrderBy(company => company.Name)
               .Take(10)
               .ToList();
    }

    private IReadOnlyList<Company> GetSelectedCompanies(IEnumerable<int> selectedIds)
    {
        return Companies
               .Where(company => selectedIds.Contains(company.Id))
               .OrderBy(company => company.Name)
               .ToList();
    }

    private static void AddSelectedCompany(List<int> selectedIds, int companyId)
    {
        if (selectedIds.Contains(companyId))
        {
            return;
        }

        selectedIds.Add(companyId);
        selectedIds.Sort();
    }

    private static string GetCompanyBadge(Company company)
    {
        return company.IgdbId.HasValue ? "Shared IGDB company" : "Shared local company";
    }
}

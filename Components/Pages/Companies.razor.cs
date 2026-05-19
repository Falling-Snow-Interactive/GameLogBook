using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Companies : CollectionPageBase<Company>
{
    private List<Game> games = [];
    private Dictionary<int, List<string>> gameNamesByCompanyId = [];
    private Dictionary<int, HashSet<GameCompanyRole>> rolesByCompanyId = [];

    private long? newCompanyIgdbId;
    private string newCompanyName = string.Empty;
    private string newCompanyCoverUrl = string.Empty;

    protected override DbSet<Company> EntitySet => DbContext.Companies;

    protected override string GetSortKey(Company item)
    {
        return item.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await LoadGameCompanySummaries();
    }

    protected override void CloseAddPopup()
    {
        base.CloseAddPopup();
        ResetForm();
    }

    private void HandleCompanySelected(Company company)
    {
        newCompanyIgdbId = company.IgdbId;
        newCompanyName = company.Name;
        newCompanyCoverUrl = company.CoverUrl ?? string.Empty;
    }

    private async Task AddCompany()
    {
        Company? existingCompany = null;

        if (newCompanyIgdbId.HasValue)
        {
            existingCompany = await DbContext.Companies
                                             .FirstOrDefaultAsync(company => company.IgdbId == newCompanyIgdbId.Value);
        }

        if (existingCompany is null && !string.IsNullOrWhiteSpace(newCompanyName))
        {
            existingCompany = await DbContext.Companies
                                             .FirstOrDefaultAsync(company => company.IgdbId == null
                                                                             && company.Name == newCompanyName.Trim());
        }

        if (existingCompany is not null)
        {
            existingCompany.Name = newCompanyName.Trim();
            existingCompany.CoverUrl = string.IsNullOrWhiteSpace(newCompanyCoverUrl)
                                           ? existingCompany.CoverUrl
                                           : newCompanyCoverUrl.Trim();
            existingCompany.LastSyncedAt = DateTimeOffset.UtcNow;
            await DbContext.SaveChangesAsync();
            await LoadItemsAsync();
            await LoadGameCompanySummaries();
            CloseAddPopup();
            return;
        }

        Company company = new()
                          {
                              IgdbId = newCompanyIgdbId,
                              Name = newCompanyName.Trim(),
                              CoverUrl = string.IsNullOrWhiteSpace(newCompanyCoverUrl)
                                             ? null
                                             : newCompanyCoverUrl.Trim(),
                              LastSyncedAt = DateTimeOffset.UtcNow
                          };

        await AddItemAsync(company);
        await LoadGameCompanySummaries();
        CloseAddPopup();
    }

    private async Task HandleRemove(Company company)
    {
        if (CompanyHasLinkedGames(company))
        {
            return;
        }

        await RemoveItemAsync(company);
        await LoadGameCompanySummaries();
    }

    private IReadOnlyList<string> GetGameNames(Company company)
    {
        return gameNamesByCompanyId.GetValueOrDefault(company.Id) ?? [];
    }

    private string GetCompanyRoleSummary(Company company)
    {
        if (!rolesByCompanyId.TryGetValue(company.Id, out HashSet<GameCompanyRole>? roles)
            || roles.Count == 0)
        {
            return "Metadata";
        }

        return string.Join(" · ", roles.OrderBy(role => role).Select(role => role.ToString()));
    }

    private string GetCompanySourceSummary(Company company)
    {
        return company.IgdbId.HasValue ? "IGDB" : "Manual";
    }

    private bool CompanyHasLinkedGames(Company company)
    {
        return gameNamesByCompanyId.ContainsKey(company.Id);
    }

    private void ResetForm()
    {
        newCompanyIgdbId = null;
        newCompanyName = string.Empty;
        newCompanyCoverUrl = string.Empty;
    }

    public bool TryGetLocalCompany(long? igdbId, out Company? company)
    {
        if (!igdbId.HasValue)
        {
            company = null;
            return false;
        }

        company = DbContext.Companies.FirstOrDefault(company => company.IgdbId == igdbId.Value);
        return company != null;
    }

    private async Task LoadGameCompanySummaries()
    {
        games = await DbContext.Games
                               .Include(game => game.Companies)
                               .ThenInclude(gameCompany => gameCompany.Company)
                               .OrderBy(game => game.Name)
                               .ToListAsync();

        gameNamesByCompanyId = games
                               .SelectMany(game => game.Companies.Select(gameCompany => new
                                                                                        {
                                                                                            game.Name,
                                                                                            gameCompany.CompanyId
                                                                                        }))
                               .GroupBy(item => item.CompanyId)
                               .ToDictionary(group => group.Key,
                                             group => group.Select(item => item.Name)
                                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                                           .OrderBy(name => name)
                                                           .ToList());

        rolesByCompanyId = games
                           .SelectMany(game => game.Companies.Select(gameCompany => new
                                                                                    {
                                                                                        gameCompany.CompanyId,
                                                                                        gameCompany.Role
                                                                                    }))
                           .GroupBy(item => item.CompanyId)
                           .ToDictionary(group => group.Key,
                                         group => group.Select(item => item.Role).ToHashSet());
    }
}

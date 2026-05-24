using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Companies : CollectionPageBase<Company>
{
    private List<Game> games = [];
    private Dictionary<int, List<string>> gameNamesByCompanyId = [];
    private Dictionary<int, HashSet<string>> rolesByCompanyId = [];
    private Company? selectedCompany;

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

    private async Task AddCompany(Company newCompany)
    {
        Company? existingCompany = null;

        if (newCompany.IgdbId.HasValue)
        {
            existingCompany = await DbContext.Companies
                                             .FirstOrDefaultAsync(company => company.IgdbId == newCompany.IgdbId.Value);
        }

        if (existingCompany is null && !string.IsNullOrWhiteSpace(newCompany.Name))
        {
            existingCompany = await DbContext.Companies
                                             .FirstOrDefaultAsync(company => company.IgdbId == null
                                                                             && company.Name == newCompany.Name.Trim());
        }

        if (existingCompany is not null)
        {
            existingCompany.Name = newCompany.Name.Trim();
            existingCompany.ImagePath = string.IsNullOrWhiteSpace(newCompany.ImagePath)
                                            ? existingCompany.ImagePath
                                            : newCompany.ImagePath.Trim();
            existingCompany.LastSyncedAt = DateTimeOffset.UtcNow;
            await DbContext.SaveChangesAsync();
            await LoadItemsAsync();
            await LoadGameCompanySummaries();
            CloseAddPopup();
            return;
        }

        await AddItemAsync(newCompany);
        await LoadGameCompanySummaries();
        CloseAddPopup();
    }

    private async Task UpdateCompany(Company updatedCompany)
    {
        if (selectedCompany is null)
        {
            return;
        }

        Company? existingCompany = await DbContext.Companies
                                                  .FirstOrDefaultAsync(company => company.ID == selectedCompany.ID);

        if (existingCompany is null)
        {
            CloseEditPopup();
            return;
        }

        existingCompany.IgdbId = updatedCompany.IgdbId;
        existingCompany.Name = updatedCompany.Name.Trim();
        existingCompany.ImagePath = string.IsNullOrWhiteSpace(updatedCompany.ImagePath)
                                        ? null
                                        : updatedCompany.ImagePath.Trim();
        existingCompany.LastSyncedAt = DateTimeOffset.UtcNow;

        await UpdateItemAsync();
        await LoadGameCompanySummaries();
        CloseEditPopup();
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
        return gameNamesByCompanyId.GetValueOrDefault(company.ID) ?? [];
    }

    private string GetCompanyRoleSummary(Company company)
    {
        if (!rolesByCompanyId.TryGetValue(company.ID, out HashSet<string>? roles)
            || roles.Count == 0)
        {
            return "Metadata";
        }

        return string.Join(" · ", roles.Order());
    }

    private string GetCompanySourceSummary(Company company)
    {
        return company.IgdbId.HasValue ? "IGDB" : "Manual";
    }

    private bool CompanyHasLinkedGames(Company company)
    {
        return gameNamesByCompanyId.ContainsKey(company.ID);
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
                               .Include(game => game.GameCompanies)
                               .OrderBy(game => game.Name)
                               .ToListAsync();

        gameNamesByCompanyId = games
                               .SelectMany(game => game.GetDeveloperIDs()
                                                       .Concat(game.GetPublisherIDs())
                                                       .Distinct()
                                                       .Select(companyId => new
                                                                            {
                                                                                game.Name,
                                                                                CompanyId = companyId
                                                                            }))
                               .GroupBy(item => item.CompanyId)
                               .ToDictionary(group => group.Key,
                                             group => group.Select(item => item.Name)
                                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                                           .OrderBy(name => name)
                                                           .ToList());

        rolesByCompanyId = games
                           .SelectMany(game => game.GetDeveloperIDs()
                                                   .Select(companyId => new
                                                                        {
                                                                            CompanyId = companyId,
                                                                            Role = "Developer"
                                                                        })
                                                   .Concat(game.GetPublisherIDs()
                                                               .Select(companyId => new
                                                                                    {
                                                                                        CompanyId = companyId,
                                                                                        Role = "Publisher"
                                                                                    })))
                           .GroupBy(item => item.CompanyId)
                           .ToDictionary(group => group.Key,
                                         group => group.Select(item => item.Role).ToHashSet());
    }

    private void OpenEditPopup(Company company)
    {
        selectedCompany = new Company
                          {
                              ID = company.ID,
                              IgdbId = company.IgdbId,
                              Name = company.Name,
                              ImagePath = company.ImagePath,
                              LastSyncedAt = company.LastSyncedAt
                          };
    }

    private void CloseEditPopup()
    {
        selectedCompany = null;
    }
}

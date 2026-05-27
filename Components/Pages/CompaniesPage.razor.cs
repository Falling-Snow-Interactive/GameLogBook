using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Elements.CompanyElements;
using VGL.Components.Popups;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Services;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Components.Pages;

public partial class CompaniesPage : CollectionPageBase<Company>
{
    private List<Game> games = [];
    private List<Platform> platforms = [];
    private Dictionary<int, List<string>> gameNamesByCompanyId = [];
    private Dictionary<int, List<string>> platformNamesByCompanyId = [];
    private Dictionary<int, HashSet<string>> rolesByCompanyId = [];

    [Inject]
    private PopupService PopupService { get; set; } = null!;

    protected override DbSet<Company> EntitySet => DbContext.Companies;

    protected override string GetSortKey(Company item)
    {
        return item.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await LoadGameCompanySummaries();
        await LoadPlatformCompanySummaries();
    }

    private async Task AddCompany(Company newCompany)
    {
        Company? existingCompany = null;

        if (newCompany.IGDB.HasValue)
        {
            existingCompany = await DbContext.Companies
                                             .FirstOrDefaultAsync(company => company.IGDB == newCompany.IGDB.Value);
        }

        if (existingCompany is null && !string.IsNullOrWhiteSpace(newCompany.Name))
        {
            existingCompany = await DbContext.Companies
                                             .FirstOrDefaultAsync(company => company.IGDB == null
                                                                             && company.Name == newCompany.Name.Trim());
        }

        if (existingCompany is not null)
        {
            ApplyCompanyDetails(existingCompany, newCompany, preserveExistingEmptyValues: true);
            await DbContext.SaveChangesAsync();
            await LoadItemsAsync();
            await LoadGameCompanySummaries();
            await LoadPlatformCompanySummaries();
            return;
        }

        await AddItemAsync(newCompany);
        await LoadGameCompanySummaries();
        await LoadPlatformCompanySummaries();
    }

    private async Task UpdateCompany(Company updatedCompany)
    {
        Company? existingCompany = await DbContext.Companies
                                                  .FirstOrDefaultAsync(company => company.ID == updatedCompany.ID);

        if (existingCompany is null)
        {
            return;
        }

        existingCompany.IGDB = updatedCompany.IGDB;
        ApplyCompanyDetails(existingCompany, updatedCompany, preserveExistingEmptyValues: false);

        await UpdateItemAsync();
        await LoadGameCompanySummaries();
        await LoadPlatformCompanySummaries();
    }

    private async Task HandleRemove(Company company)
    {
        if (CompanyHasLinks(company))
        {
            return;
        }

        await RemoveItemAsync(company);
        await LoadGameCompanySummaries();
        await LoadPlatformCompanySummaries();
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
        return company.IGDB.HasValue ? "IGDB" : "Manual";
    }

    private bool CompanyHasLinkedGames(Company company)
    {
        return gameNamesByCompanyId.ContainsKey(company.ID);
    }

    private bool CompanyHasLinkedPlatforms(Company company)
    {
        return platformNamesByCompanyId.ContainsKey(company.ID);
    }

    private bool CompanyHasLinks(Company company)
    {
        return CompanyHasLinkedGames(company)
               || CompanyHasLinkedPlatforms(company);
    }

    public bool TryGetLocalCompany(long? igdbId, out Company? company)
    {
        if (!igdbId.HasValue)
        {
            company = null;
            return false;
        }

        company = DbContext.Companies.FirstOrDefault(company => company.IGDB == igdbId.Value);
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

    private async Task LoadPlatformCompanySummaries()
    {
        platforms = await DbContext.Platforms
                                   .Include(platform => platform.PlatformCompanies)
                                   .OrderBy(platform => platform.Name)
                                   .ToListAsync();

        platformNamesByCompanyId = platforms
                                   .SelectMany(platform => platform.GetDeveloperIDs()
                                                                   .Distinct()
                                                                   .Select(companyId => new
                                                                                        {
                                                                                            platform.Name,
                                                                                            CompanyId = companyId
                                                                                        }))
                                   .GroupBy(item => item.CompanyId)
                                   .ToDictionary(group => group.Key,
                                                 group => group.Select(item => item.Name)
                                                               .Distinct(StringComparer.OrdinalIgnoreCase)
                                                               .OrderBy(name => name)
                                                               .ToList());

        foreach ((int companyId, List<string> _) in platformNamesByCompanyId)
        {
            if (!rolesByCompanyId.TryGetValue(companyId, out HashSet<string>? roles))
            {
                roles = [];
                rolesByCompanyId[companyId] = roles;
            }

            roles.Add("Platform Developer");
        }
    }

    protected override async Task OpenAddPopup()
    {
        Company? company = await PopupService.ShowAsync<AddCompanyPopup, Company>();

        if (company is not null)
        {
            await AddCompany(company);
        }
    }

    private async Task OpenEditPopup(Company company)
    {
        Company editableCompany = new()
                                  {
                                      ID = company.ID,
                                      IGDB = company.IGDB,
                                      Name = company.Name,
                                      Summary = company.Summary,
                                      FoundedDate = company.FoundedDate,
                                      Cover = CopyImageRef(company.Cover),
                                      Hero = CopyImageRef(company.Hero),
                                      Logo = CopyImageRef(company.Logo),
                                      Icon = CopyImageRef(company.Icon),
                                      ImagePath = company.ImagePath,
                                      LastSyncedAt = company.LastSyncedAt
                                  };

        Company? updatedCompany = await PopupService.ShowAsync<AddCompanyPopup, Company>(
            new Dictionary<string, object?>
            {
                [nameof(AddCompanyPopup.InitialCompany)] = editableCompany
            });

        if (updatedCompany is not null)
        {
            await UpdateCompany(updatedCompany);
        }
    }

    private static void ApplyCompanyDetails(
        Company target,
        Company source,
        bool preserveExistingEmptyValues)
    {
        target.IGDB = preserveExistingEmptyValues
                          ? source.IGDB ?? target.IGDB
                          : source.IGDB;
        target.Name = source.Name.Trim();
        target.Summary = NormalizeOptionalText(source.Summary, preserveExistingEmptyValues ? target.Summary : null);
        target.FoundedDate = preserveExistingEmptyValues
                                 ? source.FoundedDate ?? target.FoundedDate
                                 : source.FoundedDate;
        target.Cover = CopyImageRef(source.Cover) ?? (preserveExistingEmptyValues ? target.Cover : null);
        target.Hero = CopyImageRef(source.Hero) ?? (preserveExistingEmptyValues ? target.Hero : null);
        target.Logo = CopyImageRef(source.Logo) ?? (preserveExistingEmptyValues ? target.Logo : null);
        target.Icon = CopyImageRef(source.Icon) ?? (preserveExistingEmptyValues ? target.Icon : null);
        target.ImagePath = NormalizeOptionalText(source.ImagePath, preserveExistingEmptyValues ? target.ImagePath : null);
        target.LastSyncedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptionalText(string? value, string? fallback)
    {
        return string.IsNullOrWhiteSpace(value)
                   ? fallback
                   : value.Trim();
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

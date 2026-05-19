using GameLogBook.Models.Companies;
using GameLogBook.Services;
using IGDB;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements;

public partial class CompanyPicker : ComponentBase
{
    private HashSet<int> selectedCompanyIds = [];
    private string selectedCompanyId = string.Empty;

    [Inject]
    public IgdbClientProvider? IgdbClientProvider { get; set; }
    
    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];
    
    private IReadOnlyList<Company> AvailableCompanies =>
        Companies
            .Where(company => !selectedCompanyIds.Contains(company.Id))
            .OrderBy(company => company.Name)
            .ToList();

    private IReadOnlyList<Company> SelectedCompanies =>
        Companies
            .Where(company => selectedCompanyIds.Contains(company.Id))
            .OrderBy(company => company.Name)
            .ToList();

    private void HandleCompanySelected(ChangeEventArgs args)
    {
        selectedCompanyId = args.Value?.ToString() ?? string.Empty;

        if (int.TryParse(selectedCompanyId, out int companyId))
        {
            selectedCompanyIds.Add(companyId);
        }

        selectedCompanyId = string.Empty;
    }

    private void RemoveCompany(int companyId)
    {
        selectedCompanyIds.Remove(companyId);
    }
    
    private HashSet<int> GetMatchingLocalCompanyIds(IEnumerable<string> manufacturerNames)
    {
        HashSet<string> normalizedManufacturerNames = manufacturerNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Companies
               .Where(company => normalizedManufacturerNames.Contains(company.Name))
               .Select(company => company.Id)
               .ToHashSet();
    }
    
    private async Task<Dictionary<long, string>> GetCompanyNames(long[] companyIds)
    {
        if (companyIds.Length == 0)
        {
            return [];
        }

        string companyIdsFilter = string.Join(",", companyIds);

        IGDB.Models.Company[] companies = await IgdbClientProvider
                                                .GetClient()
                                                .QueryAsync<IGDB.Models.Company>(IGDBClient.Endpoints.Companies,
                                                                                 query: $"""
                                                                                         fields id, name;
                                                                                         where id = ({companyIdsFilter});
                                                                                         limit {companyIds.Length};
                                                                                         """);

        return companies
               .Where(company => company.Id.HasValue
                                 && !string.IsNullOrWhiteSpace(company.Name))
               .ToDictionary(company => company.Id!.Value,
                             company => company.Name!);
    }
}
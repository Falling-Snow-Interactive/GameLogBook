using IGDB;
using Microsoft.AspNetCore.Components;
using VGL.Models.Companies;
using VGL.Services;

namespace VGL.Components.Elements;

public partial class CompanyPicker : ComponentBase
{
    private HashSet<int> selectedCompanyIds = [];
    private string selectedCompanyId = string.Empty;

    [Inject]
    public IGDBClientProvider IgdbClientProvider { get; set; } = null!;
    
    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    [Parameter]
    public IReadOnlyCollection<int> SelectedCompanyIds { get; set; } = [];

    [Parameter]
    public EventCallback<HashSet<int>> SelectedCompanyIdsChanged { get; set; }
    
    private IReadOnlyList<Company> AvailableCompanies =>
        Companies
            .Where(company => !selectedCompanyIds.Contains(company.ID))
            .OrderBy(company => company.Name)
            .ToList();

    private IReadOnlyList<Company> SelectedCompanies =>
        Companies
            .Where(company => selectedCompanyIds.Contains(company.ID))
            .OrderBy(company => company.Name)
            .ToList();

    protected override void OnParametersSet()
    {
        selectedCompanyIds = SelectedCompanyIds.ToHashSet();
    }

    private async Task HandleCompanySelected(ChangeEventArgs args)
    {
        selectedCompanyId = args.Value?.ToString() ?? string.Empty;

        if (int.TryParse(selectedCompanyId, out int companyId))
        {
            selectedCompanyIds.Add(companyId);
            await SelectedCompanyIdsChanged.InvokeAsync(selectedCompanyIds.ToHashSet());
        }

        selectedCompanyId = string.Empty;
    }

    private async Task RemoveCompany(int companyId)
    {
        selectedCompanyIds.Remove(companyId);
        await SelectedCompanyIdsChanged.InvokeAsync(selectedCompanyIds.ToHashSet());
    }
    
    private HashSet<int> GetMatchingLocalCompanyIds(IEnumerable<string> manufacturerNames)
    {
        HashSet<string> normalizedManufacturerNames = manufacturerNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Companies
               .Where(company => normalizedManufacturerNames.Contains(company.Name))
               .Select(company => company.ID)
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

using GameLogBook.Models.Companies;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.CompanyElements;

public partial class DeveloperSearch : ComponentBase
{
    [Parameter]
    public IReadOnlyList<Company>? Companies { get; set; } = [];
    
    [Parameter]
    public string? Placeholder { get; set; }
    
    private string developerSearchText = string.Empty;
    private List<int> selectedDeveloperCompanyIDs = [];
    
    private IReadOnlyList<Company>? DeveloperMatches => FilterCompanies(developerSearchText, selectedDeveloperCompanyIDs);
    private IReadOnlyList<Company>? SelectedDeveloperCompanies => GetSelectedCompanies(selectedDeveloperCompanyIDs);
    
    private IReadOnlyList<Company>? FilterCompanies(string searchText, IReadOnlyCollection<int> selectedIds)
    {
        string trimmedSearchText = searchText.Trim();

        return Companies?
               .Where(company => !selectedIds.Contains(company.ID))
               .Where(company => string.IsNullOrWhiteSpace(trimmedSearchText)
                                 || company.Name.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase))
               .OrderBy(company => company.Name)
               .Take(10)
               .ToList();
    }

    private void SelectDeveloper(Company company)
    {
        AddSelectedCompany(selectedDeveloperCompanyIDs, company.ID);
        developerSearchText = string.Empty;
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
    
    private List<Company>? GetSelectedCompanies(IEnumerable<int> selectedIds)
    {
        return Companies?
               .Where(company => selectedIds.Contains(company.ID))
               .OrderBy(company => company.Name)
               .ToList();
    }
    
    private void RemoveDeveloper(int companyId)
    {
        selectedDeveloperCompanyIDs.Remove(companyId);
    }
    
    private static string GetCompanyBadge(Company company)
    {
        return company.IgdbId.HasValue ? "Shared IGDB company" : "Shared local company";
    }
    
    
}
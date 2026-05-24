using GameLogBook.Models.Companies;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.CompanyElements;

public partial class CompanySearch : ComponentBase
{
    [Parameter]
    public IReadOnlyList<Company>? Companies { get; set; } = [];
    
    [Parameter]
    public string? Placeholder { get; set; }
    
    [Parameter]
    public string SearchText { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> SearchTextChanged { get; set; }

    private List<int> selectedCompanyIDs = [];

    private IReadOnlyList<Company>? CompanyMatches => FilterCompanies(SearchText, selectedCompanyIDs);
    private IReadOnlyList<Company>? SelectedCompanies => GetSelectedCompanies(selectedCompanyIDs);

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

    private async Task SelectCompany(Company company)
    {
        AddSelectedCompany(selectedCompanyIDs, company.ID);
        await SetSearchTextAsync(string.Empty);
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
    
    private void RemoveCompany(int companyId)
    {
        selectedCompanyIDs.Remove(companyId);
    }
    
    private static string GetCompanyBadge(Company company)
    {
        return company.IgdbId.HasValue ? "Shared IGDB company" : "Shared local company";
    }
    
    #region Search

    private async Task OnSearchTextChanged(ChangeEventArgs e)
    {
        await SetSearchTextAsync(e.Value?.ToString() ?? string.Empty);
    }

    private async Task SetSearchTextAsync(string value)
    {
        SearchText = value;
        await SearchTextChanged.InvokeAsync(SearchText);
    }
    
    #endregion

    private void HandlePlusClicked()
    {
        
    }
}
using Microsoft.AspNetCore.Components;
using VGL.Components.Popups;
using VGL.Models.Companies;
using VGL.Services;

namespace VGL.Components.Elements.CompanyElements;

public partial class CompanySearch : ComponentBase
{
    private const int FocusOutDelayMilliseconds = 250;

    private List<int> selectedCompanyIDs = [];
    private bool isDropdownActive;
    private int focusChangeVersion;

    [Inject]
    private PopupService PopupService { get; set; } = null!;

    [Parameter]
    public IReadOnlyList<Company>? Companies { get; set; } = [];
    
    [Parameter]
    public string? Placeholder { get; set; }
    
    [Parameter]
    public string SearchText { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> SearchTextChanged { get; set; }

    [Parameter]
    public List<int> SelectedCompanyIds { get; set; } = [];

    [Parameter]
    public EventCallback<List<int>> SelectedCompanyIdsChanged { get; set; }

    [Parameter]
    public Func<Company, Task<Company?>>? OnCompanyAdded { get; set; }

    private bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
    private bool ShouldShowDropdown => isDropdownActive && HasSearchText;
    private IReadOnlyList<Company> CompanyMatches => HasSearchText
                                                         ? FilterCompanies(SearchText, selectedCompanyIDs)
                                                         : [];
    private IReadOnlyList<Company>? SelectedCompanies => GetSelectedCompanies(selectedCompanyIDs);

    protected override void OnParametersSet()
    {
        selectedCompanyIDs = SelectedCompanyIds
                             .Distinct()
                             .Order()
                             .ToList();
    }

    private IReadOnlyList<Company> FilterCompanies(string searchText, IReadOnlyCollection<int> selectedIds)
    {
        string trimmedSearchText = searchText.Trim();

        return Companies?
               .Where(company => !selectedIds.Contains(company.ID))
               .Where(company => company.Name.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase))
               .OrderBy(company => company.Name)
               .Take(10)
               .ToList()
               ?? [];
    }

    private async Task SelectCompany(Company company)
    {
        await AddSelectedCompanyAsync(company.ID);
        await SetSearchTextAsync(string.Empty);
    }
    
    private async Task AddSelectedCompanyAsync(int companyId)
    {
        if (selectedCompanyIDs.Contains(companyId))
        {
            return;
        }

        selectedCompanyIDs.Add(companyId);
        selectedCompanyIDs.Sort();
        await SelectedCompanyIdsChanged.InvokeAsync([..selectedCompanyIDs]);
    }
    
    private List<Company>? GetSelectedCompanies(IEnumerable<int> selectedIds)
    {
        return Companies?
               .Where(company => selectedIds.Contains(company.ID))
               .OrderBy(company => company.Name)
               .ToList();
    }
    
    private async Task RemoveCompany(int companyId)
    {
        selectedCompanyIDs.Remove(companyId);
        await SelectedCompanyIdsChanged.InvokeAsync([..selectedCompanyIDs]);
    }
    
    private static string GetCompanyBadge(Company company)
    {
        return company.IGDB.HasValue ? "Shared IGDB company" : "Shared local company";
    }
    
    #region Search

    private async Task OnSearchTextChanged(ChangeEventArgs e)
    {
        MarkDropdownActive();
        await SetSearchTextAsync(e.Value?.ToString() ?? string.Empty);
    }

    private async Task SetSearchTextAsync(string value)
    {
        SearchText = value;
        await SearchTextChanged.InvokeAsync(SearchText);
    }
    
    #endregion

    private void HandleSearchFocusIn()
    {
        MarkDropdownActive();
    }

    private async Task HandleSearchFocusOut()
    {
        int currentFocusChangeVersion = ++focusChangeVersion;
        await Task.Delay(FocusOutDelayMilliseconds);

        if (currentFocusChangeVersion != focusChangeVersion)
        {
            return;
        }

        isDropdownActive = false;
        await InvokeAsync(StateHasChanged);
    }

    private void MarkDropdownActive()
    {
        focusChangeVersion++;
        isDropdownActive = true;
    }

    private async Task HandlePlusClicked()
    {
        MarkDropdownActive();
        Company? company = await PopupService.ShowAsync<AddCompanyPopup, Company>();

        if (company is not null)
        {
            await HandleCompanySaved(company);
        }
    }

    private async Task HandleCompanySaved(Company company)
    {
        if (OnCompanyAdded is null)
        {
            return;
        }

        Company? savedCompany = await OnCompanyAdded.Invoke(company);

        if (savedCompany is not null)
        {
            await AddSelectedCompanyAsync(savedCompany.ID);
        }
    }
}

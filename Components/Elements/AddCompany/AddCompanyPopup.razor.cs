using GameLogBook.Models.Companies;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.AddCompany;

public partial class AddCompanyPopup
{
    private Company? previousInitialCompany;
    private long? companyIgdbId;
    private string companyName = string.Empty;
    private string companyCoverUrl = string.Empty;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Company> OnCompanySaved { get; set; }

    [Parameter]
    public Company? InitialCompany { get; set; }

    private string PopupTitle => InitialCompany is null ? "Add Company" : "Edit Company";

    private string SaveButtonText => InitialCompany is null ? "Add Company" : "Save Changes";

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(previousInitialCompany, InitialCompany))
        {
            return;
        }

        previousInitialCompany = InitialCompany;

        if (InitialCompany is null)
        {
            ResetForm();
            return;
        }

        LoadCompany(InitialCompany);
    }

    private void HandleCompanySelected(Company company)
    {
        LoadCompany(company);
    }

    private async Task HandleSaveCompany()
    {
        Company company = new()
                          {
                              Id = InitialCompany?.Id ?? 0,
                              IgdbId = companyIgdbId,
                              Name = companyName.Trim(),
                              CoverUrl = string.IsNullOrWhiteSpace(companyCoverUrl)
                                             ? null
                                             : companyCoverUrl.Trim(),
                              LastSyncedAt = DateTimeOffset.UtcNow
                          };

        await OnCompanySaved.InvokeAsync(company);
    }

    private void LoadCompany(Company company)
    {
        companyIgdbId = company.IgdbId;
        companyName = company.Name;
        companyCoverUrl = company.CoverUrl ?? string.Empty;
    }

    private void ResetForm()
    {
        companyIgdbId = null;
        companyName = string.Empty;
        companyCoverUrl = string.Empty;
    }
}

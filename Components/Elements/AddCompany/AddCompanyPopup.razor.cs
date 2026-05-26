using GameLogBook.Models.Companies;
using GameLogBook.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace GameLogBook.Components.Elements.AddCompany;

public partial class AddCompanyPopup
{
    private Company? previousInitialCompany;
    
    // Information
    private string name = string.Empty;
    private string summary = string.Empty;
    
    // Images
    private string companyImagePath = string.Empty;
    private string companyImageUrl = string.Empty;
    private string? companyPreviewSource;
    private IBrowserFile? uploadedCompanyImage;
    private string? imageErrorMessage;
    
    // Other APIs
    private long? igdb;
    
    // Saving
    private bool isSaving;

    [Inject]
    private LocalImageService LocalImageService { get; set; } = null!;

    [CascadingParameter]
    private PopupInstance? Popup { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Company> OnCompanySaved { get; set; }

    [Parameter]
    public Company? InitialCompany { get; set; }

    private string PopupTitle => InitialCompany is null ? "Add Company" : "Edit Company";

    private string SaveButtonText => InitialCompany is null ? "Add Company" : "Save Changes";

    protected override async Task OnParametersSetAsync()
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

        await LoadCompany(InitialCompany);
    }

    private async Task HandleCompanySelected(Company company)
    {
        await LoadCompany(company);
    }

    private async Task HandleSaveCompany()
    {
        isSaving = true;
        imageErrorMessage = null;

        string? imagePath;
        try
        {
            imagePath = await ResolveCompanyImagePath();
        }
        catch (Exception exception)
        {
            imageErrorMessage = exception.Message;
            isSaving = false;
            return;
        }

        Company company = new()
                          {
                              ID = InitialCompany?.ID ?? 0,
                              IGDB = igdb,
                              Name = name.Trim(),
                              ImagePath = imagePath,
                              LastSyncedAt = DateTimeOffset.UtcNow
                          };

        if (Popup is not null)
        {
            await Popup.CloseAsync(company);
        }
        else
        {
            await OnCompanySaved.InvokeAsync(company);
        }

        isSaving = false;
    }

    private async Task HandleClose()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync();
            return;
        }

        await OnClose.InvokeAsync();
    }

    private async Task LoadCompany(Company company)
    {
        igdb = company.IGDB;
        name = company.Name;
        companyImagePath = company.ImagePath ?? string.Empty;
        companyImageUrl = company.PendingImageUrl ?? string.Empty;
        uploadedCompanyImage = null;
        imageErrorMessage = null;
        companyPreviewSource = !string.IsNullOrWhiteSpace(companyImageUrl)
                                   ? companyImageUrl
                                   : await LocalImageService.GetImageSourceAsync(companyImagePath);
    }

    private void ResetForm()
    {
        igdb = null;
        name = string.Empty;
        companyImagePath = string.Empty;
        companyImageUrl = string.Empty;
        companyPreviewSource = null;
        uploadedCompanyImage = null;
        imageErrorMessage = null;
        isSaving = false;
    }

    private async Task HandleCompanyImageUrlChanged(ChangeEventArgs args)
    {
        companyImageUrl = args.Value?.ToString() ?? string.Empty;
        uploadedCompanyImage = null;
        imageErrorMessage = null;
        companyPreviewSource = !string.IsNullOrWhiteSpace(companyImageUrl)
                                   ? companyImageUrl
                                   : await LocalImageService.GetImageSourceAsync(companyImagePath);
    }

    private async Task HandleCompanyImageFileSelected(InputFileChangeEventArgs args)
    {
        uploadedCompanyImage = args.File;
        companyImageUrl = string.Empty;
        imageErrorMessage = null;

        try
        {
            companyPreviewSource = await LocalImageService.GetUploadPreviewSourceAsync(uploadedCompanyImage);
        }
        catch (Exception exception)
        {
            uploadedCompanyImage = null;
            companyPreviewSource = await LocalImageService.GetImageSourceAsync(companyImagePath);
            imageErrorMessage = exception.Message;
        }
    }

    private void RemoveCompanyImage()
    {
        companyImagePath = string.Empty;
        companyImageUrl = string.Empty;
        companyPreviewSource = null;
        uploadedCompanyImage = null;
        imageErrorMessage = null;
    }

    private async Task<string?> ResolveCompanyImagePath()
    {
        if (uploadedCompanyImage is not null)
        {
            return await LocalImageService.SaveUploadedImageAsync(uploadedCompanyImage, "companies");
        }

        if (!string.IsNullOrWhiteSpace(companyImageUrl))
        {
            return await LocalImageService.DownloadImageAsync(companyImageUrl, "companies");
        }

        return string.IsNullOrWhiteSpace(companyImagePath) ? null : companyImagePath;
    }
}

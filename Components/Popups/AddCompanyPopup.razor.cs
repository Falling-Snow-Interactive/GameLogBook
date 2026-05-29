using Microsoft.AspNetCore.Components;
using VGL.Components.Elements.ImageField;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Services;

namespace VGL.Components.Popups;

public partial class AddCompanyPopup
{
    private Company? previousInitialCompany;
    
    // Information
    private string name = string.Empty;
    private string summary = string.Empty;
    private DateOnly? foundedDate;
    
    // Images
    private ImageFieldWidget? coverField;
    private ImageFieldWidget? heroField;
    private ImageFieldWidget? logoField;
    private ImageFieldWidget? iconField;

    private ImageRef? cover;
    private ImageRef? hero;
    private ImageRef? logo;
    private ImageRef? icon;
    private string? imageErrorMessage;
    
    // Other APIs
    private long? igdb;
    
    // Saving
    private bool isSaving;

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

        try
        {
            ImageRef? coverRef = await coverField?.CommitAsync()!;
            ImageRef? heroRef = await heroField?.CommitAsync()!;
            ImageRef? logoRef = await logoField?.CommitAsync()!;
            ImageRef? iconRef = await iconField?.CommitAsync()!;

            string? legacyImagePath = logoRef?.Path
                                      ?? iconRef?.Path
                                      ?? coverRef?.Path
                                      ?? heroRef?.Path;

            Company company = new()
                              {
                                  ID = InitialCompany?.ID ?? 0,
                                  IGDB = igdb,
                                  Name = name.Trim(),
                                  Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                                  FoundedDate = foundedDate,
                                  Cover = coverRef,
                                  Hero = heroRef,
                                  Logo = logoRef,
                                  Icon = iconRef,
                                  ImagePath = legacyImagePath,
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
        catch (Exception exception)
        {
            imageErrorMessage = exception.Message;
            isSaving = false;
        }
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

    private Task LoadCompany(Company company)
    {
        igdb = company.IGDB ?? igdb;
        name = company.Name;
        summary = company.Summary ?? summary;
        foundedDate = company.FoundedDate ?? foundedDate;

        cover = CheckOverrideImage(cover, company.Cover);
        // hero = CheckOverrideImage(hero, company.Cover);
        // logo = CheckOverrideImage(cover, company.Cover);
        // icon = CheckOverrideImage(cover, company.Cover);

        imageErrorMessage = null;

        return Task.CompletedTask;
    }

    private ImageRef? CheckOverrideImage(ImageRef? imageRef, ImageRef? newRef)
    {
        if (newRef == null)
        {
            return imageRef;
        }
        
        if (imageRef == null)
        {
            return newRef;
        }

        return newRef.IsValid() ? newRef : imageRef;
    }

    private void ResetForm()
    {
        igdb = null;
        name = string.Empty;
        summary = string.Empty;
        foundedDate = null;

        cover = null;
        hero = null;
        logo = null;
        icon = null;

        imageErrorMessage = null;
        isSaving = false;
    }

    private static ImageRef? GetLogoImageRef(Company company)
    {
        if (company.Logo is not null)
        {
            company.Logo.PendingUrl = company.PendingImageUrl;
            return company.Logo;
        }

        if (string.IsNullOrWhiteSpace(company.ImagePath)
            && string.IsNullOrWhiteSpace(company.PendingImageUrl))
        {
            return null;
        }

        return new ImageRef
               {
                   Path = company.ImagePath,
                   PendingUrl = company.PendingImageUrl
               };
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using GameLogBook.Services;
using Company = GameLogBook.Models.Companies.Company;
using Cover = GameLogBook.Models.Games.Cover;
using Game = GameLogBook.Models.Games.Game;

namespace GameLogBook.Components.Elements.AddGame;

public partial class AddGamePopup
{
    private Game? previousInitialGame;

    [Inject]
    private LocalImageService LocalImageService { get; set; } = null!;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Game> OnGameSelected { get; set; }

    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    [Parameter]
    public Game? InitialGame { get; set; }

    private List<int> selectedDeveloperCompanyIds = [];
    private List<int> selectedPublisherCompanyIds = [];
    private string developerSearchText = string.Empty;
    private string publisherSearchText = string.Empty;

    private string gameName = string.Empty;
    private long igdbId;
    private DateOnly? releaseDate;
    private string coverImagePath = string.Empty;
    private string coverImageUrl = string.Empty;
    private string? coverPreviewSource;
    private IBrowserFile? uploadedCoverFile;
    private string? imageErrorMessage;
    private bool isSaving;
    private string summary = string.Empty;

    private string PopupTitle => InitialGame is null ? "Add Game" : "Edit Game";

    private string SaveButtonText => InitialGame is null ? "Add to Library" : "Save Changes";

    protected override async Task OnParametersSetAsync()
    {
        if (ReferenceEquals(previousInitialGame, InitialGame))
        {
            return;
        }

        previousInitialGame = InitialGame;

        if (InitialGame is null)
        {
            ResetForm();
            return;
        }

        await LoadGame(InitialGame);
    }

    private async Task HandleGameSelected(Game game)
    {
        await LoadGame(game);
    }

    private async Task HandleSaveGame()
    {
        isSaving = true;
        imageErrorMessage = null;

        string? imagePath;
        try
        {
            imagePath = await ResolveCoverImagePath();
        }
        catch (Exception exception)
        {
            imageErrorMessage = exception.Message;
            isSaving = false;
            return;
        }

        Game game = new()
                    {
                        Id = InitialGame?.Id ?? 0,
                        IgdbId = igdbId,
                        Name = gameName.Trim(),
                        ReleaseDate = releaseDate,
                        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                        Cover = string.IsNullOrWhiteSpace(imagePath)
                                    ? null
                                    : new Cover
                                      {
                                          ImagePath = imagePath
                                      },
                        DeveloperCompanyIds = selectedDeveloperCompanyIds.Distinct().ToArray(),
                        PublisherCompanyIds = selectedPublisherCompanyIds.Distinct().ToArray()
                    };

        await OnGameSelected.InvokeAsync(game);
        isSaving = false;
    }

    private async Task LoadGame(Game game)
    {
        igdbId = game.IgdbId;
        gameName = game.Name;
        selectedDeveloperCompanyIds = ResolveLocalCompanyIds(game.DeveloperCompanyIds);
        selectedPublisherCompanyIds = ResolveLocalCompanyIds(game.PublisherCompanyIds);
        developerSearchText = string.Empty;
        publisherSearchText = string.Empty;
        releaseDate = game.ReleaseDate;
        coverImagePath = game.Cover?.ImagePath ?? string.Empty;
        coverImageUrl = game.Cover?.PendingImageUrl ?? string.Empty;
        uploadedCoverFile = null;
        imageErrorMessage = null;
        coverPreviewSource = !string.IsNullOrWhiteSpace(coverImageUrl)
                                 ? coverImageUrl
                                 : await LocalImageService.GetImageSourceAsync(coverImagePath);
        summary = game.Summary ?? string.Empty;
    }

    private void ResetForm()
    {
        selectedDeveloperCompanyIds = [];
        selectedPublisherCompanyIds = [];
        developerSearchText = string.Empty;
        publisherSearchText = string.Empty;
        gameName = string.Empty;
        igdbId = 0;
        releaseDate = null;
        coverImagePath = string.Empty;
        coverImageUrl = string.Empty;
        coverPreviewSource = null;
        uploadedCoverFile = null;
        imageErrorMessage = null;
        isSaving = false;
        summary = string.Empty;
    }

    private async Task HandleCoverUrlChanged(ChangeEventArgs args)
    {
        coverImageUrl = args.Value?.ToString() ?? string.Empty;
        uploadedCoverFile = null;
        imageErrorMessage = null;
        coverPreviewSource = !string.IsNullOrWhiteSpace(coverImageUrl)
                                 ? coverImageUrl
                                 : await LocalImageService.GetImageSourceAsync(coverImagePath);
    }

    private async Task HandleCoverFileSelected(InputFileChangeEventArgs args)
    {
        uploadedCoverFile = args.File;
        coverImageUrl = string.Empty;
        imageErrorMessage = null;

        try
        {
            coverPreviewSource = await LocalImageService.GetUploadPreviewSourceAsync(uploadedCoverFile);
        }
        catch (Exception exception)
        {
            uploadedCoverFile = null;
            coverPreviewSource = await LocalImageService.GetImageSourceAsync(coverImagePath);
            imageErrorMessage = exception.Message;
        }
    }

    private void RemoveCoverImage()
    {
        coverImagePath = string.Empty;
        coverImageUrl = string.Empty;
        coverPreviewSource = null;
        uploadedCoverFile = null;
        imageErrorMessage = null;
    }

    private async Task<string?> ResolveCoverImagePath()
    {
        if (uploadedCoverFile is not null)
        {
            return await LocalImageService.SaveUploadedImageAsync(uploadedCoverFile, "games");
        }

        if (!string.IsNullOrWhiteSpace(coverImageUrl))
        {
            return await LocalImageService.DownloadImageAsync(coverImageUrl, "games");
        }

        return string.IsNullOrWhiteSpace(coverImagePath) ? null : coverImagePath;
    }

    private void SelectDeveloper(Company company)
    {
        AddSelectedCompany(selectedDeveloperCompanyIds, company.ID);
        developerSearchText = string.Empty;
    }

    private void SelectPublisher(Company company)
    {
        AddSelectedCompany(selectedPublisherCompanyIds, company.ID);
        publisherSearchText = string.Empty;
    }

    private void RemoveDeveloper(int companyId)
    {
        selectedDeveloperCompanyIds.Remove(companyId);
    }

    private void RemovePublisher(int companyId)
    {
        selectedPublisherCompanyIds.Remove(companyId);
    }

    private List<int> ResolveLocalCompanyIds(IEnumerable<int> companyIds)
    {
        return companyIds
               .Where(companyId => Companies.Any(company => company.ID == companyId))
               .Distinct()
               .Order()
               .ToList();
    }

    private IReadOnlyList<Company> DeveloperMatches => FilterCompanies(developerSearchText, selectedDeveloperCompanyIds);

    private IReadOnlyList<Company> PublisherMatches => FilterCompanies(publisherSearchText, selectedPublisherCompanyIds);

    private IReadOnlyList<Company> SelectedDeveloperCompanies => GetSelectedCompanies(selectedDeveloperCompanyIds);

    private IReadOnlyList<Company> SelectedPublisherCompanies => GetSelectedCompanies(selectedPublisherCompanyIds);

    private IReadOnlyList<Company> FilterCompanies(string searchText, IReadOnlyCollection<int> selectedIds)
    {
        string trimmedSearchText = searchText.Trim();

        return Companies
               .Where(company => !selectedIds.Contains(company.ID))
               .Where(company => string.IsNullOrWhiteSpace(trimmedSearchText)
                                  || company.Name.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase))
               .OrderBy(company => company.Name)
               .Take(10)
               .ToList();
    }

    private IReadOnlyList<Company> GetSelectedCompanies(IEnumerable<int> selectedIds)
    {
        return Companies
               .Where(company => selectedIds.Contains(company.ID))
               .OrderBy(company => company.Name)
               .ToList();
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

    private static string GetCompanyBadge(Company company)
    {
        return company.IgdbId.HasValue ? "Shared IGDB company" : "Shared local company";
    }
}

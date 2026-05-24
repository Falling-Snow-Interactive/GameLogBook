using GameLogBook.Models.Games;
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
    public Func<Company, Task<Company?>>? OnCompanyAdded { get; set; }

    [Parameter]
    public Game? InitialGame { get; set; }

    private List<int> selectedDeveloperCompanyIDs = [];
    private List<int> selectedPublisherCompanyIDs = [];
    private string developerSearchText = string.Empty;
    private string publisherSearchText = string.Empty;

    private string gameName = string.Empty;
    private long? igdb;
    private DateOnly? releaseDate;
    private string coverImagePath = string.Empty;
    private string coverImageUrl = string.Empty;
    private string? coverPreviewSource;
    private IBrowserFile? uploadedCoverFile;
    private string? imageErrorMessage;
    private bool isSaving;
    private string summary = string.Empty;
    private GameType gameType;

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
                        ID = InitialGame?.ID ?? 0,
                        IgdbId = igdb,
                        Name = gameName.Trim(),
                        GameType = gameType,
                        ReleaseDate = releaseDate,
                        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                        Cover = string.IsNullOrWhiteSpace(imagePath)
                                    ? null
                                    : new Cover
                                      {
                                          ImagePath = imagePath
                                      },
                    };
        
        game.AddCompaniesByID(GameCompanyRole.Developer, selectedDeveloperCompanyIDs);
        game.AddCompaniesByID(GameCompanyRole.Publisher, selectedPublisherCompanyIDs);

        await OnGameSelected.InvokeAsync(game);
        isSaving = false;
    }

    private async Task LoadGame(Game game)
    {
        igdb = game.IgdbId;
        gameName = game.Name;
        selectedDeveloperCompanyIDs = ResolveLocalCompanyIds(game.GetDeveloperIDs());
        selectedPublisherCompanyIDs = ResolveLocalCompanyIds(game.GetPublisherIDs());
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
        gameType = game.GameType;
    }

    private void ResetForm()
    {
        selectedDeveloperCompanyIDs = [];
        selectedPublisherCompanyIDs = [];
        developerSearchText = string.Empty;
        publisherSearchText = string.Empty;
        gameName = string.Empty;
        igdb = 0;
        releaseDate = null;
        coverImagePath = string.Empty;
        coverImageUrl = string.Empty;
        coverPreviewSource = null;
        uploadedCoverFile = null;
        imageErrorMessage = null;
        isSaving = false;
        summary = string.Empty;
        gameType = GameType.None;
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

    private List<int> ResolveLocalCompanyIds(IEnumerable<int> companyIds)
    {
        return companyIds
               .Where(companyId => Companies.Any(company => company.ID == companyId))
               .Distinct()
               .Order()
               .ToList();
    }
}

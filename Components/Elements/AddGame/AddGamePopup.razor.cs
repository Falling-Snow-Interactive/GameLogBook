using Microsoft.AspNetCore.Components;
using Company = GameLogBook.Models.Companies.Company;
using Cover = GameLogBook.Models.Games.Cover;
using Game = GameLogBook.Models.Games.Game;

namespace GameLogBook.Components.Elements.AddGame;

public partial class AddGamePopup
{
    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Game> OnGameSelected { get; set; }

    private List<Company> companies = [];
    private int? selectedDeveloperCompanyId;
    private int? selectedPublisherCompanyId;

    private string gameName = string.Empty;
    private long igdbId;
    private string? developer = string.Empty;
    private string? publisher = string.Empty;
    private DateOnly? releaseDate;
    private string coverUrl = string.Empty;
    private string summary = string.Empty;

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private Task HandleGameSelected(Game game)
    {
        igdbId = game.IgdbId;
        gameName = game.Name;
        developer = game.Developer;
        publisher = game.Publisher;
        releaseDate = game.ReleaseDate;
        coverUrl = game.Cover?.Url ?? string.Empty;
        summary = game.Summary ?? string.Empty;

        return Task.CompletedTask;
    }

    private async Task HandleSaveGame()
    {
        Game game = new()
                    {
                        IgdbId = igdbId,
                        Name = gameName.Trim(),
                        Developer = string.IsNullOrWhiteSpace(developer) ? null : developer.Trim(),
                        Publisher = string.IsNullOrWhiteSpace(publisher) ? null : publisher.Trim(),
                        ReleaseDate = releaseDate,
                        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
                        Cover = string.IsNullOrWhiteSpace(coverUrl)
                                    ? null
                                    : new Cover
                                      {
                                          Url = coverUrl.Trim()
                                      }
                    };

        await OnGameSelected.InvokeAsync(game);
    }
}

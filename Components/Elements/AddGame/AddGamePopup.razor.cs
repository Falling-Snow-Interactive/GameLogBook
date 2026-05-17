using GameLogBook.Models;
using GameLogBook.Models.Library;
using GameLogBook.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GameLogBook.Components.Elements.AddGame;

public partial class AddGamePopup
{
    [Inject]
    protected IgdbService IgdbService { get; set; } = null!;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Game> OnGameSelected { get; set; }

    private string searchInput = string.Empty;
    private List<Game> searchResults = [];
    private bool isSearching;
    private string? errorMessage;

    private string gameName = string.Empty;
    private string? developer = string.Empty;
    private string? publisher = string.Empty;
    private DateOnly? releaseDate;
    private string coverUrl = string.Empty;
    private string summary = string.Empty;

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private async Task HandleSearch()
    {
        string trimmedSearchInput = searchInput.Trim();

        if (string.IsNullOrWhiteSpace(trimmedSearchInput))
        {
            return;
        }

        isSearching = true;
        errorMessage = null;
        searchResults.Clear();

        try
        {
            IReadOnlyList<Game> results = await IgdbService.SearchGamesAsync(trimmedSearchInput);
            searchResults = results.ToList();
        }
        catch (Exception exception)
        {
            errorMessage = $"Search failed: {exception.Message}";
        }
        finally
        {
            isSearching = false;
        }
    }

    private Task HandleGameSelected(Game game)
    {
        gameName = game.Name;
        developer = game.Developer;
        publisher = game.Publisher;
        releaseDate = game.ReleaseDate;
        coverUrl = game.Cover?.Url ?? string.Empty;
        summary = game.Summary ?? string.Empty;
        
        searchResults.Clear();

        return Task.CompletedTask;
    }

    private async Task HandleSaveGame()
    {
        Game game = new()
                    {
                        Name = gameName.Trim(),
                        Developer = developer?.Trim(),
                        Publisher = publisher?.Trim(),
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
    
    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or "NumpadEnter")
        {
            await HandleSearch();
        }
    }
}
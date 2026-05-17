using GameLogBook.Models.Games;
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
    private List<Game> searchResults = new List<Game>();
    private bool isSearching;
    private string? errorMessage;

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

    private async Task HandleGameSelected(Game game)
    {
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
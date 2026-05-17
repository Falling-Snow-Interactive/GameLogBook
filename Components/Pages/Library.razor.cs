using System.Text.Json;
using GameLogBook.Models.Games;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GameLogBook.Components.Pages;

public partial class Library
{
    private const string SessionStorageKey = "game-log-book-library";

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;
    
    private readonly List<Game> games = [];

    private bool isAddPopupOpen;

    private void OpenAddPopup()
    {
        isAddPopupOpen = true;
    }

    private void CloseAddPopup()
    {
        isAddPopupOpen = false;
    }

    private async Task AddGame(Game game)
    {
        games.Add(game);
        CloseAddPopup();

        await SaveLibraryToSessionStorageAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await LoadLibraryFromSessionStorageAsync();
        StateHasChanged();
    }

    private async Task LoadLibraryFromSessionStorageAsync()
    {
        string? json = await JsRuntime.InvokeAsync<string?>("sessionStorage.getItem",
                                                            SessionStorageKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        List<Game>? savedGames = JsonSerializer.Deserialize<List<Game>>(json);

        if (savedGames is null)
        {
            return;
        }

        games.Clear();
        games.AddRange(savedGames);
    }

    private async Task SaveLibraryToSessionStorageAsync()
    {
        string json = JsonSerializer.Serialize(games);

        await JsRuntime.InvokeVoidAsync("sessionStorage.setItem",
                                        SessionStorageKey,
                                        json);
    }
}
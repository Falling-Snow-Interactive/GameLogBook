using Microsoft.AspNetCore.Components;
using VGL.Models.Games;

namespace VGL.Components.Elements.GameElements;

public partial class GameSearch : ComponentBase
{
    private const int FocusOutDelayMilliseconds = 250;

    private bool isDropdownActive;
    private int focusChangeVersion;
    private readonly List<Game> addedGames = [];

    [Parameter]
    public IReadOnlyList<Game> Games { get; set; } = [];

    [Parameter]
    public string Placeholder { get; set; } = "Search games...";

    [Parameter]
    public int? SelectedGameId { get; set; }

    [Parameter]
    public EventCallback<int?> SelectedGameIdChanged { get; set; }

    [Parameter]
    public Func<Task<Game?>>? OnGameAdded { get; set; }

    private string SearchText { get; set; } = string.Empty;

    private bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

    private bool ShouldShowDropdown => isDropdownActive && HasSearchText;

    private Game? SelectedGame => SelectedGameId is null
                                      ? null
                                      : AllGames.FirstOrDefault(game => game.ID == SelectedGameId.Value);

    private IEnumerable<Game> AllGames => Games
                                          .Concat(addedGames)
                                          .DistinctBy(game => game.ID);

    private IReadOnlyList<Game> GameMatches => HasSearchText
                                                   ? FilterGames(SearchText)
                                                   : [];

    private IReadOnlyList<Game> FilterGames(string searchText)
    {
        string trimmedSearchText = searchText.Trim();

        return AllGames
               .Where(game => game.ID != SelectedGameId)
               .Where(game => game.Name.Contains(trimmedSearchText, StringComparison.OrdinalIgnoreCase))
               .OrderBy(game => game.Name)
               .Take(10)
               .ToList();
    }

    private async Task SelectGame(Game game)
    {
        await SelectedGameIdChanged.InvokeAsync(game.ID);
        SearchText = string.Empty;
        isDropdownActive = false;
    }

    private async Task RemoveGame()
    {
        await SelectedGameIdChanged.InvokeAsync(null);
    }

    private async Task HandlePlusClicked()
    {
        MarkDropdownActive();

        if (OnGameAdded is null)
        {
            return;
        }

        Game? game = await OnGameAdded.Invoke();

        if (game is null)
        {
            return;
        }

        AddOrUpdateAddedGame(game);
        await SelectedGameIdChanged.InvokeAsync(game.ID);
        SearchText = string.Empty;
        isDropdownActive = false;
    }

    private void AddOrUpdateAddedGame(Game game)
    {
        int existingIndex = addedGames.FindIndex(existingGame => existingGame.ID == game.ID);

        if (existingIndex >= 0)
        {
            addedGames[existingIndex] = game;
            return;
        }

        addedGames.Add(game);
    }

    private async Task OnSearchTextChanged(ChangeEventArgs args)
    {
        MarkDropdownActive();
        SearchText = args.Value?.ToString() ?? string.Empty;
        await InvokeAsync(StateHasChanged);
    }

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

    private static string GetGameBadge(Game game)
    {
        return game.ReleaseDate is null
                   ? game.GameType.ToString()
                   : $"{game.GameType} · {game.ReleaseDate:MMM d, yyyy}";
    }
}

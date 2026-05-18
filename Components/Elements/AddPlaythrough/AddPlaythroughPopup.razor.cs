using GameLogBook.Models;
using GameLogBook.Models.Library;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.AddPlaythrough;

public partial class AddPlaythroughPopup : ComponentBase
{
    private string? playthroughName;
    private string? errorMessage;
    private int? selectedGameIdToAdd;
    private readonly List<int> selectedGameIds = [];

    [Parameter]
    public IReadOnlyList<Game> LibraryGames { get; set; } = [];

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Playthrough> OnPlaythroughSelected { get; set; }

    private IEnumerable<Game> SelectedGames =>
        LibraryGames.Where(game => selectedGameIds.Contains(game.Id));

    private IEnumerable<Game> AvailableGamesToAdd =>
        LibraryGames.Where(game => !selectedGameIds.Contains(game.Id));

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private void HandleSelectedGameChanged(ChangeEventArgs args)
    {
        string? value = args.Value?.ToString();

        selectedGameIdToAdd = int.TryParse(value, out int gameId)
         
                                  ? gameId
            : null;
    }

    private void HandleAddSelectedGame()
    {
        if (selectedGameIdToAdd is null)
        {
            return;
        }

        int gameId = selectedGameIdToAdd.Value;

        if (!selectedGameIds.Contains(gameId))
        {
            selectedGameIds.Add(gameId);
        }

        selectedGameIdToAdd = null;
    }

    private void HandleRemoveGame(int gameId)
    {
        selectedGameIds.Remove(gameId);

        if (selectedGameIdToAdd == gameId)
        {
            selectedGameIdToAdd = null;
        }
    }

    private async Task HandleSavePlaythrough()
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(playthroughName))
        {
            errorMessage = "Please enter a playthrough name.";
            return;
        }

        Playthrough playthrough = new()
                                  {
                                      Name = playthroughName.Trim(),
                                      GameIds = selectedGameIds.ToArray()
                                  };

        await OnPlaythroughSelected.InvokeAsync(playthrough);
    }
}
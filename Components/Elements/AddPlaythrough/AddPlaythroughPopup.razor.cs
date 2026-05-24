using GameLogBook.Models;
using GameLogBook.Models.Games;
using GameLogBook.Services;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.AddPlaythrough;

public partial class AddPlaythroughPopup : ComponentBase
{
    private Playthrough? previousInitialPlaythrough;
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

    [Parameter]
    public Playthrough? InitialPlaythrough { get; set; }

    [CascadingParameter]
    private PopupInstance? Popup { get; set; }

    private string PopupTitle => InitialPlaythrough is null ? "Add Playthrough" : "Edit Playthrough";

    private string SaveButtonText => InitialPlaythrough is null ? "Add Playthrough" : "Save Changes";

    private IEnumerable<Game> SelectedGames =>
        LibraryGames.Where(game => selectedGameIds.Contains(game.ID));

    private IEnumerable<Game> AvailableGamesToAdd =>
        LibraryGames.Where(game => !selectedGameIds.Contains(game.ID));

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(previousInitialPlaythrough, InitialPlaythrough))
        {
            return;
        }

        previousInitialPlaythrough = InitialPlaythrough;

        if (InitialPlaythrough is null)
        {
            ResetForm();
            return;
        }

        LoadPlaythrough(InitialPlaythrough);
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
                                      ID = InitialPlaythrough?.ID ?? 0,
                                      Name = playthroughName.Trim(),
                                      GameIds = selectedGameIds.ToArray()
                                  };

        if (Popup is not null)
        {
            await Popup.CloseAsync(playthrough);
        }
        else
        {
            await OnPlaythroughSelected.InvokeAsync(playthrough);
        }
    }

    private void LoadPlaythrough(Playthrough playthrough)
    {
        playthroughName = playthrough.Name;
        errorMessage = null;
        selectedGameIdToAdd = null;
        selectedGameIds.Clear();
        selectedGameIds.AddRange(playthrough.GameIds.Distinct());
    }

    private void ResetForm()
    {
        playthroughName = string.Empty;
        errorMessage = null;
        selectedGameIdToAdd = null;
        selectedGameIds.Clear();
    }
}

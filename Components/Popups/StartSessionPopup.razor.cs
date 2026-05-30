using Microsoft.AspNetCore.Components;
using VGL.Models;
using VGL.Models.Games;
using VGL.Services;
using VGL.Services.NowPlaying;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Popups;

public partial class StartSessionPopup : ComponentBase
{
    private int? selectedPlaythroughId;
    private int? selectedGameId;
    private int? selectedPlatformId;
    private string newPlaythroughName = string.Empty;
    private string? errorMessage;

    [Parameter]
    public IReadOnlyList<Playthrough> Playthroughs { get; set; } = [];

    [Parameter]
    public IReadOnlyList<Game> LibraryGames { get; set; } = [];

    [Parameter]
    public IReadOnlyList<PlatformModel> Platforms { get; set; } = [];

    [Parameter]
    public Playthrough? InitialPlaythrough { get; set; }

    [Parameter]
    public Game? InitialGame { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [CascadingParameter]
    private PopupInstance? Popup { get; set; }

    private bool CanStart => selectedGameId is not null
                             && selectedPlatformId is not null
                             && (selectedPlaythroughId is not null || !string.IsNullOrWhiteSpace(newPlaythroughName));

    protected override void OnParametersSet()
    {
        selectedPlaythroughId = InitialPlaythrough?.ID;
        selectedGameId = InitialPlaythrough?.GameID ?? InitialGame?.ID;
        selectedPlatformId = InitialPlaythrough?.PlatformID;
        newPlaythroughName = InitialGame is null ? "New Playthrough" : $"{InitialGame.Name} Playthrough";

        if (selectedPlaythroughId is null && Playthroughs.Count == 1)
        {
            selectedPlaythroughId = Playthroughs[0].ID;
            ApplyPlaythrough(Playthroughs[0]);
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

    private void HandlePlaythroughChanged(ChangeEventArgs args)
    {
        selectedPlaythroughId = int.TryParse(args.Value?.ToString(), out int parsed) ? parsed : null;
        Playthrough? playthrough = Playthroughs.FirstOrDefault(item => item.ID == selectedPlaythroughId);

        if (playthrough is not null)
        {
            ApplyPlaythrough(playthrough);
            return;
        }

        selectedGameId ??= InitialGame?.ID;
    }

    private async Task HandleStart()
    {
        errorMessage = null;

        if (!CanStart)
        {
            errorMessage = "Choose a game, platform, and playthrough.";
            return;
        }

        StartSessionRequest request = new()
                                      {
                                          ExistingPlaythroughID = selectedPlaythroughId,
                                          GameID = selectedGameId,
                                          PlatformID = selectedPlatformId,
                                          NewPlaythroughName = selectedPlaythroughId is null ? newPlaythroughName : null,
                                      };

        if (Popup is not null)
        {
            await Popup.CloseAsync(request);
        }
    }

    private void ApplyPlaythrough(Playthrough playthrough)
    {
        selectedGameId = playthrough.GameID ?? selectedGameId;
        selectedPlatformId = playthrough.PlatformID ?? selectedPlatformId;
    }
}

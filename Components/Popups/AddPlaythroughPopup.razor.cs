using Microsoft.AspNetCore.Components;
using System.Globalization;
using VGL.Models;
using VGL.Models.Games;
using VGL.Services;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Popups;

public partial class AddPlaythroughPopup : ComponentBase
{
    private const string DateTimeInputFormat = "yyyy-MM-ddTHH:mm";

    private Playthrough? previousInitialPlaythrough;
    private string playthroughName = string.Empty;
    private string? errorMessage;
    private int? selectedGameId;
    private int? selectedPlatformId;
    private int? selectedRunId;
    private string newRunName = string.Empty;
    private PlaythroughStatus status;
    private DateTime? manualStartedAt;
    private DateTime? manualFinishedAt;
    private DateTime? manualMasteredAt;

    [Parameter]
    public IReadOnlyList<Game> LibraryGames { get; set; } = [];

    [Parameter]
    public IReadOnlyList<PlatformModel> Platforms { get; set; } = [];

    [Parameter]
    public IReadOnlyList<PlaythroughRun> PlaythroughRuns { get; set; } = [];

    [Parameter]
    public Func<Task<Game?>>? OnGameAdded { get; set; }

    [Parameter]
    public Func<Task<PlatformModel?>>? OnPlatformAdded { get; set; }

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

    private bool CanSave => !string.IsNullOrWhiteSpace(playthroughName)
                            && selectedGameId is not null
                            && selectedPlatformId is not null;

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

    private void HandleRunChanged(ChangeEventArgs args)
    {
        selectedRunId = ParseNullableInt(args.Value);
    }

    private void HandleManualStartedAtChanged(ChangeEventArgs args)
    {
        manualStartedAt = ParseDateTimeInput(args.Value?.ToString());
    }

    private void HandleManualFinishedAtChanged(ChangeEventArgs args)
    {
        manualFinishedAt = ParseDateTimeInput(args.Value?.ToString());
    }

    private void HandleManualMasteredAtChanged(ChangeEventArgs args)
    {
        manualMasteredAt = ParseDateTimeInput(args.Value?.ToString());
    }

    private async Task HandleSavePlaythrough()
    {
        errorMessage = null;

        if (!CanSave)
        {
            errorMessage = "Enter a name, game, and platform.";
            return;
        }

        Playthrough playthrough = new()
                                  {
                                      ID = InitialPlaythrough?.ID ?? 0,
                                      Name = playthroughName.Trim(),
                                      Status = status,
                                      GameID = selectedGameId,
                                      PlatformID = selectedPlatformId,
                                      PlaythroughRunID = selectedRunId,
                                      ManualStartedAt = ToDateTimeOffset(manualStartedAt),
                                      ManualFinishedAt = ToDateTimeOffset(manualFinishedAt),
                                      ManualMasteredAt = ToDateTimeOffset(manualMasteredAt)
                                  };

        if (!string.IsNullOrWhiteSpace(newRunName))
        {
            playthrough.PlaythroughRun = new PlaythroughRun
                                         {
                                             Name = newRunName.Trim()
                                         };
        }

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
        status = playthrough.Status;
        selectedGameId = playthrough.GameID;
        selectedPlatformId = playthrough.PlatformID;
        selectedRunId = playthrough.PlaythroughRunID;
        newRunName = string.Empty;
        manualStartedAt = ToLocalDateTime(playthrough.ManualStartedAt);
        manualFinishedAt = ToLocalDateTime(playthrough.ManualFinishedAt);
        manualMasteredAt = ToLocalDateTime(playthrough.ManualMasteredAt);
        errorMessage = null;
    }

    private void ResetForm()
    {
        playthroughName = string.Empty;
        status = PlaythroughStatus.NotStarted;
        selectedGameId = null;
        selectedPlatformId = null;
        selectedRunId = null;
        newRunName = string.Empty;
        manualStartedAt = null;
        manualFinishedAt = null;
        manualMasteredAt = null;
        errorMessage = null;
    }

    private static int? ParseNullableInt(object? value)
    {
        return int.TryParse(value?.ToString(), out int parsed) ? parsed : null;
    }

    private static DateTimeOffset? ToDateTimeOffset(DateTime? value)
    {
        return value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Local));
    }

    private static DateTime? ToLocalDateTime(DateTimeOffset? value)
    {
        return value?.LocalDateTime;
    }

    private static string FormatDateTimeInput(DateTime? value)
    {
        return value?.ToString(DateTimeInputFormat, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static DateTime? ParseDateTimeInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] supportedFormats =
        [
            DateTimeInputFormat,
            "yyyy-MM-ddTHH:mm:ss"
        ];

        return DateTime.TryParseExact(value,
                                      supportedFormats,
                                      CultureInfo.InvariantCulture,
                                      DateTimeStyles.None,
                                      out DateTime parsed)
                   ? parsed
                   : null;
    }
}

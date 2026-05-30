using Microsoft.AspNetCore.Components;
using System.Globalization;
using VGL.Models;
using VGL.Models.Games;
using VGL.Services;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Popups;

public partial class AddGameLogPopup : ComponentBase
{
    private const string DateTimeInputFormat = "yyyy-MM-ddTHH:mm";

    private GameLog? previousInitialLog;
    private string title = string.Empty;
    private string location = string.Empty;
    private string notes = string.Empty;
    private string? errorMessage;
    private int? selectedPlaythroughId;
    private int? selectedGameId;
    private int? selectedPlatformId;
    private DateTime? startedAt = DateTime.Now;
    private DateTime? endedAt = DateTime.Now.AddHours(1);
    private PlaythroughStatus? statusChange;

    [Parameter]
    public IReadOnlyList<Playthrough> Playthroughs { get; set; } = [];

    [Parameter]
    public IReadOnlyList<Game> LibraryGames { get; set; } = [];

    [Parameter]
    public IReadOnlyList<PlatformModel> Platforms { get; set; } = [];

    [Parameter]
    public Func<Task<Game?>>? OnGameAdded { get; set; }

    [Parameter]
    public Func<Task<PlatformModel?>>? OnPlatformAdded { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<GameLog> OnLogSelected { get; set; }

    [Parameter]
    public GameLog? InitialLog { get; set; }

    [CascadingParameter]
    private PopupInstance? Popup { get; set; }

    private string PopupTitle => InitialLog is null ? "Add Log" : "Edit Log";

    private string SaveButtonText => InitialLog is null ? "Add Log" : "Save Changes";

    private string statusChangeValue => statusChange?.ToString() ?? string.Empty;

    private bool CanSave => selectedPlaythroughId is not null
                            && selectedGameId is not null
                            && selectedPlatformId is not null
                            && startedAt is not null;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(previousInitialLog, InitialLog))
        {
            return;
        }

        previousInitialLog = InitialLog;

        if (InitialLog is null)
        {
            ResetForm();
            return;
        }

        LoadLog(InitialLog);
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
        selectedPlaythroughId = ParseNullableInt(args.Value);
        Playthrough? playthrough = Playthroughs.FirstOrDefault(item => item.ID == selectedPlaythroughId);

        if (playthrough is null)
        {
            return;
        }

        selectedGameId = playthrough.GameID ?? selectedGameId;
        selectedPlatformId = playthrough.PlatformID ?? selectedPlatformId;

        if (statusChange is null && playthrough.Status == PlaythroughStatus.NotStarted)
        {
            statusChange = PlaythroughStatus.Playing;
        }
    }

    private void HandleStatusChangeChanged(ChangeEventArgs args)
    {
        string? value = args.Value?.ToString();
        statusChange = Enum.TryParse(value, out PlaythroughStatus parsed) ? parsed : null;
    }

    private void HandleStartedAtChanged(ChangeEventArgs args)
    {
        startedAt = ParseDateTimeInput(args.Value?.ToString());
    }

    private void HandleEndedAtChanged(ChangeEventArgs args)
    {
        endedAt = ParseDateTimeInput(args.Value?.ToString());
    }

    private async Task HandleSaveLog()
    {
        errorMessage = null;

        if (!CanSave)
        {
            errorMessage = "Choose a playthrough, game, platform, and start time.";
            return;
        }

        if (endedAt is not null && endedAt < startedAt)
        {
            errorMessage = "End time must be after start time.";
            return;
        }

        GameLog log = new()
                      {
                          ID = InitialLog?.ID ?? 0,
                          PlaythroughID = selectedPlaythroughId!.Value,
                          GameID = selectedGameId!.Value,
                          PlatformID = selectedPlatformId!.Value,
                          Title = Normalize(title),
                          StartedAt = ToDateTimeOffset(startedAt!.Value),
                          EndedAt = endedAt is null ? null : ToDateTimeOffset(endedAt.Value),
                          Location = Normalize(location),
                          Notes = Normalize(notes),
                          StatusChange = statusChange
                      };

        if (Popup is not null)
        {
            await Popup.CloseAsync(log);
        }
        else
        {
            await OnLogSelected.InvokeAsync(log);
        }
    }

    private void LoadLog(GameLog log)
    {
        title = log.Title ?? string.Empty;
        location = log.Location ?? string.Empty;
        notes = log.Notes ?? string.Empty;
        selectedPlaythroughId = log.PlaythroughID;
        selectedGameId = log.GameID;
        selectedPlatformId = log.PlatformID;
        startedAt = log.StartedAt.LocalDateTime;
        endedAt = log.EndedAt?.LocalDateTime;
        statusChange = log.StatusChange;
        errorMessage = null;
    }

    private void ResetForm()
    {
        title = string.Empty;
        location = string.Empty;
        notes = string.Empty;
        selectedPlaythroughId = null;
        selectedGameId = null;
        selectedPlatformId = null;
        startedAt = DateTime.Now;
        endedAt = DateTime.Now.AddHours(1);
        statusChange = null;
        errorMessage = null;
    }

    private static int? ParseNullableInt(object? value)
    {
        return int.TryParse(value?.ToString(), out int parsed) ? parsed : null;
    }

    private static string? Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Local));
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

using Microsoft.AspNetCore.Components;
using VGL.Models;
using VGL.Services.NowPlaying;
using VGL.Services.UserProfiles;

namespace VGL.Components.Pages;

public partial class NowPlayingPage : ComponentBase, IDisposable
{
    private CancellationTokenSource? notesSaveCts;
    private GameLog? session;
    private bool isLoading = true;
    private string notes = string.Empty;
    private string? saveStatus;
    private PeriodicTimer? timer;
    private CancellationTokenSource? timerCts;

    [Parameter]
    public int? LogId { get; set; }

    [Inject]
    private NowPlayingSessionService NowPlaying { get; set; } = null!;

    [Inject]
    private UserProfileSession UserSession { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private bool IsEnded => session?.EndedAt is not null;

    private string statusChangeValue => session?.StatusChange?.ToString() ?? string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        if (!UserSession.IsSignedIn && !await UserSession.TryAutoSignInAsync())
        {
            Navigation.NavigateTo("/profiles");
            return;
        }

        await LoadSessionAsync();
        StartTimer();
    }

    private async Task LoadSessionAsync()
    {
        isLoading = true;
        session = await NowPlaying.GetSessionAsync(LogId);
        notes = session?.Notes ?? string.Empty;
        saveStatus = null;
        isLoading = false;
    }

    private void HandleNotesInput(ChangeEventArgs args)
    {
        notes = args.Value?.ToString() ?? string.Empty;
        QueueNotesSave();
    }

    private void QueueNotesSave()
    {
        if (session is null || IsEnded)
        {
            return;
        }

        notesSaveCts?.Cancel();
        notesSaveCts?.Dispose();
        notesSaveCts = new CancellationTokenSource();
        _ = SaveNotesAfterDelayAsync(session.ID, notesSaveCts.Token);
    }

    private async Task SaveNotesAfterDelayAsync(int logId, CancellationToken cancellationToken)
    {
        try
        {
            saveStatus = "Saving notes...";
            await InvokeAsync(StateHasChanged);
            await Task.Delay(800, cancellationToken);
            await NowPlaying.SaveNotesAsync(logId, notes);
            saveStatus = "Notes saved";
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SaveNotesNowAsync()
    {
        if (session is null)
        {
            return;
        }

        notesSaveCts?.Cancel();
        notesSaveCts?.Dispose();
        notesSaveCts = null;
        await NowPlaying.SaveNotesAsync(session.ID, notes);
    }

    private async Task HandleStatusChanged(ChangeEventArgs args)
    {
        if (session is null || IsEnded)
        {
            return;
        }

        PlaythroughStatus? statusChange = Enum.TryParse(args.Value?.ToString(), out PlaythroughStatus parsed)
                                               ? parsed
                                               : null;

        await NowPlaying.SaveStatusChangeAsync(session.ID, statusChange);
        session = await NowPlaying.GetSessionAsync(session.ID);
        saveStatus = "Status saved";
    }

    private async Task HandleEndSession()
    {
        if (session is null || IsEnded)
        {
            return;
        }

        await SaveNotesNowAsync();
        session = await NowPlaying.EndSessionAsync(session.ID);
        saveStatus = "Session ended";
    }

    private void StartTimer()
    {
        timerCts?.Cancel();
        timerCts?.Dispose();
        timer?.Dispose();

        if (session is null || IsEnded)
        {
            return;
        }

        timerCts = new CancellationTokenSource();
        timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _ = RunTimerAsync(timerCts.Token);
    }

    private async Task RunTimerAsync(CancellationToken cancellationToken)
    {
        if (timer is null)
        {
            return;
        }

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string GetSessionTitle(GameLog log)
    {
        return string.IsNullOrWhiteSpace(log.Title) ? $"{log.Game.Name} session" : log.Title;
    }

    private static string FormatDateTime(DateTimeOffset value)
    {
        return value.LocalDateTime.ToString("MMM d, yyyy h:mm tt");
    }

    private static string FormatElapsed(GameLog log)
    {
        DateTimeOffset end = log.EndedAt ?? DateTimeOffset.Now;
        TimeSpan duration = end >= log.StartedAt ? end - log.StartedAt : TimeSpan.Zero;
        int hours = duration.Days * 24 + duration.Hours;

        if (hours > 0)
        {
            return $"{hours}h {duration.Minutes}m {duration.Seconds}s";
        }

        return $"{duration.Minutes}m {duration.Seconds}s";
    }

    public void Dispose()
    {
        notesSaveCts?.Cancel();
        notesSaveCts?.Dispose();
        timerCts?.Cancel();
        timerCts?.Dispose();
        timer?.Dispose();
    }
}

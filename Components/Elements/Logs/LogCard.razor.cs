using Microsoft.AspNetCore.Components;
using VGL.Data;
using VGL.Models;
using VGL.Services.NowPlaying;

namespace VGL.Components.Elements.Logs;

public partial class LogCard : ComponentBase
{
    [Inject]
    protected GameLogBookDbContext DbContext { get; set; } = null!;
    
    [Inject]
    private NowPlayingSessionService NowPlaying { get; set; } = null!;
    
    // Variables
    [Parameter, EditorRequired]
    public GameLog Log { get; set; }
    
    // Callbacks
    
    [Parameter]
    public EventCallback Refresh { get; set; }
    
    [Parameter]
    public EventCallback<GameLog> OpenEditPopup { get; set; }
    
    #region UI Interactions

    private async Task EditButton_Clicked()
    {
        await OpenEditPopup.InvokeAsync(Log);
    }
    
    private async Task RemoveButton_Clicked()
    {
        int playthroughId = Log.PlaythroughID;
        DbContext.GameLogs.Remove(Log);
        
        await NowPlaying.RecalculatePlaythroughStatusAsync(playthroughId);
        await Refresh.InvokeAsync();
    }

    private void StartButton_Clicked()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        Log.StartedAt = now;

        if (Log.EndedAt is null || Log.EndedAt.Value < Log.StartedAt)
        {
            Log.EndedAt = Log.StartedAt;
        }
    }

    private void EndButton_Clicked()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        Log.EndedAt = now;

        if (Log.EndedAt is not null && Log.StartedAt > Log.EndedAt.Value)
        {
            Log.StartedAt = Log.EndedAt.Value;
        }
    }
    
    #endregion
}
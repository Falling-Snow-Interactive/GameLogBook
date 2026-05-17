using GameLogBook.Models.Library;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.GameEntry;

public partial class GameEntry
{
    [Parameter]
    public Game Game { get; set; } = null!;

    [Parameter]
    public bool ShowButtons { get; set; } = false;

    [Parameter]
    public EventCallback OnClick { get; set; }
    
    [Parameter]
    public EventCallback OnPlaythroughs { get; set; }
    
    [Parameter]
    public EventCallback OnLogs { get; set; }
    
    [Parameter]
    public EventCallback<Game> OnRemove { get; set; }

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }

    private async Task HandlePlaythroughs()
    {
        await OnPlaythroughs.InvokeAsync();
    }

    private async Task HandleLogs()
    {
        await OnLogs.InvokeAsync();
    }

    private async Task HandleRemove()
    {
        await OnRemove.InvokeAsync(Game);
    }
}
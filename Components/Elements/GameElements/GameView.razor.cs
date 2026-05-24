using GameLogBook.Models.Games;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.GameElements;

public partial class GameView : ComponentBase
{
    [Parameter]
    public Game Game { get; set; } = null;
    
    [Parameter]
    public EventCallback OnClose { get; set; }
    
    [Parameter]
    public EventCallback OnEdit { get; set; }

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private async Task HandleEdit()
    {
        await OnEdit.InvokeAsync();
    }
}
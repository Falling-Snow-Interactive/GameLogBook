using GameLogBook.Models.Games;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.GameElements.GameView;

public partial class GameView : ComponentBase
{
    [Parameter]
    public Game Game { get; set; } = new();

    [Parameter]
    public EventCallback<Game> OnEdit { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    private async Task HandleEdit()
    {
        await OnEdit.InvokeAsync(Game);
    }

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }
}
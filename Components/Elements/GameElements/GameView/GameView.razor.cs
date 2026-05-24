using GameLogBook.Models.Games;
using GameLogBook.Services;
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

    [CascadingParameter]
    private PopupInstance? Popup { get; set; }

    private async Task HandleEdit()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync(true);
            return;
        }

        await OnEdit.InvokeAsync(Game);
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
}

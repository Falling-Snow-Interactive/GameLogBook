using Microsoft.AspNetCore.Components;
using VGL.Models.Games;
using VGL.Services;

namespace VGL.Components.Elements.GameElements;

public partial class GameView : ComponentBase
{
    [Parameter, EditorRequired]
    public Game Game { get; set; }

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

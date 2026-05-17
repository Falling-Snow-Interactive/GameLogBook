using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.AddGame;

public partial class AddGameButton : ComponentBase
{
    [Parameter]
    public EventCallback OnClick { get; set; }
    
    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }
}
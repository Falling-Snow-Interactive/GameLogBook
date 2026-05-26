using Microsoft.AspNetCore.Components;

namespace VGL.Components.Elements.GameElements;

public partial class AddGameButton : ComponentBase
{
    [Parameter]
    public EventCallback OnClick { get; set; }
    
    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }
}
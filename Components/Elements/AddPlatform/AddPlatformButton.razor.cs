using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.AddPlatform;

public partial class AddPlatformButton : ComponentBase
{
    [Parameter]
    public EventCallback OnClick { get; set; }
    
    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }
}
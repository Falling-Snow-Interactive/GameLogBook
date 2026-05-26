using Microsoft.AspNetCore.Components;

namespace VGL.Components.Elements.PlatformElements;

public partial class AddPlatformButton : ComponentBase
{
    [Parameter]
    public EventCallback OnClick { get; set; }
    
    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }
}
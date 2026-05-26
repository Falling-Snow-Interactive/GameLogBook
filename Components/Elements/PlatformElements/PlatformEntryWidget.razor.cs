using Microsoft.AspNetCore.Components;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Components.Elements.PlatformElements;

public partial class PlatformEntryWidget : ComponentBase
{
    [Parameter]
    public Platform Platform { get; set; } = null!;

    [Parameter]
    public EventCallback<Platform> OnClick { get; set; }
    
    [Parameter]
    public EventCallback<Platform> OnRemove { get; set; }

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync(Platform);
    }

    private async Task HandleRemove()
    {
        await OnRemove.InvokeAsync(Platform);
    }
}
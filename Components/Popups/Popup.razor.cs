using Microsoft.AspNetCore.Components;
using VGL.Services;

namespace VGL.Components.Popups;

public partial class Popup : ComponentBase
{
    [Parameter]
    public string Title { get; set; } = string.Empty;
    
    [Parameter]
    public string? Description { get; set; }
    
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    [Parameter]
    public EventCallback OnClose { get; set; }

    [CascadingParameter]
    private PopupInstance? PopupInstance { get; set; }

    private async Task HandleClose()
    {
        if (PopupInstance is not null)
        {
            await PopupInstance.CloseAsync();
            return;
        }

        await OnClose.InvokeAsync();
    }
}

using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements;

public partial class Popup : ComponentBase
{
    [Parameter]
    public string Title { get; set; }
    
    [Parameter]
    public string? Description { get; set; }
    
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    [Parameter]
    public EventCallback OnClose { get; set; }

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }
}
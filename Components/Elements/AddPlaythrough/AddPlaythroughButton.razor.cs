using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.AddPlaythrough;

public partial class AddPlaythroughButton : ComponentBase
{
    [Parameter]
    public EventCallback OnClick { get; set; }
    
    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }
}
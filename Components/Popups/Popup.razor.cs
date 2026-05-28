using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VGL.Services;

namespace VGL.Components.Popups;

public partial class Popup : ComponentBase, IAsyncDisposable
{
    private ElementReference popupElement;
    private bool focusObserverConnected;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await JsRuntime.InvokeVoidAsync("gameLogBookFocus.observeModal", popupElement);
        focusObserverConnected = true;
    }

    private async Task HandleClose()
    {
        if (PopupInstance is not null)
        {
            await PopupInstance.CloseAsync();
            return;
        }

        await OnClose.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (!focusObserverConnected)
        {
            return;
        }

        try
        {
            await JsRuntime.InvokeVoidAsync("gameLogBookFocus.cleanupModal", popupElement);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}

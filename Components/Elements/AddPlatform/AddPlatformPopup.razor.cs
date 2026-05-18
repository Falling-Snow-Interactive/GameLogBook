using GameLogBook.Models.Platforms;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.AddPlatform;

public partial class AddPlatformPopup : ComponentBase
{
    private string platformName = string.Empty;
    private string? errorMessage;

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<Platform> OnPlatformSelected { get; set; }

    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }

    private async Task HandleSavePlatform()
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(platformName))
        {
            errorMessage = "Please enter a platform name.";
            return;
        }

        Platform platform = new()
                            {
                                Name = platformName.Trim()
                            };

        await OnPlatformSelected.InvokeAsync(platform);
    }
}

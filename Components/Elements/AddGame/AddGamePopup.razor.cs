using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.AddGame;

public partial class AddGamePopup
{
    [Parameter]
    public EventCallback OnClose { get; set; }
    
    [Parameter]
    public EventCallback<string> OnSave { get; set; }
    
    private string gameNameInput = string.Empty;
    
    private async Task HandleClose()
    {
        await OnClose.InvokeAsync();
    }
    
    private async Task HandleSave()
    {
        string trimmedGameName = gameNameInput.Trim();

        if (string.IsNullOrWhiteSpace(trimmedGameName))
        {
            return;
        }

        await OnSave.InvokeAsync(trimmedGameName);
    }
}
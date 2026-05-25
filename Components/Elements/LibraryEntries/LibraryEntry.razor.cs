using GameLogBook.Models.Libraries.Entries;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.LibraryEntries;

public partial class LibraryEntry : ComponentBase
{
    [Parameter]
    public ILibraryEntry Entry { get; set; } = null!;

    [Parameter]
    public EventCallback<ILibraryEntry> OnClick { get; set; }
    
    [Parameter]
    public EventCallback<ILibraryEntry> OnRemove { get; set; }

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync(Entry);
    }

    private async Task HandleRemove()
    {
        await OnRemove.InvokeAsync(Entry);
    }
}
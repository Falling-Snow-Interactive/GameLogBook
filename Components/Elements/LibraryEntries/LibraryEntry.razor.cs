using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using GameLogBook.Models.Libraries.Entries;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.LibraryEntries;

public partial class LibraryEntry : ComponentBase
{
    [Parameter]
    public ILibraryEntry Entry { get; set; } = null!;
    
    [Parameter]
    public List<Game>? RelatedGames { get; set; }
    
    [Parameter]
    public List<Company>? RelatedCompanies { get; set; }

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
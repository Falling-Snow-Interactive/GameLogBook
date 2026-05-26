using Microsoft.AspNetCore.Components;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Models.Libraries.Entries;

namespace VGL.Components.Elements.LibraryEntries;

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
using GameLogBook.Data;
using GameLogBook.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Playthroughs : ComponentBase
{
    [Inject]
    private GameLogBookDbContext DbContext { get; set; } = null!;
    
    private List<Playthrough> playthroughs = [];

    private bool isAddPopupOpen;
    
    protected override async Task OnInitializedAsync()
    {
        playthroughs = await DbContext.Playthroughs.ToListAsync();
    }

    private void OpenAddPopup()
    {
        isAddPopupOpen = true;
    }

    private void CloseAddPopup()
    {
        isAddPopupOpen = false;
    }

    private async Task AddPlaythrough(Playthrough playthrough)
    {
        DbContext.Playthroughs.Add(playthrough);
        await DbContext.SaveChangesAsync();

        playthroughs.Add(playthrough);
        CloseAddPopup();
    }

    private async Task HandleRemove(Playthrough playthrough)
    {
        DbContext.Playthroughs.Remove(playthrough);
        await DbContext.SaveChangesAsync();

        playthroughs.Remove(playthrough);
    }
}
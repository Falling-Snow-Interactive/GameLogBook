using GameLogBook.Data;
using GameLogBook.Models;
using GameLogBook.Models.Games;
using GameLogBook.Models.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Library
{
    [Inject]
    private GameLogBookDbContext DbContext { get; set; } = null!;
    
    private List<Game> games = [];

    private bool isAddPopupOpen;
    
    protected override async Task OnInitializedAsync()
    {
        games = await DbContext.Games.ToListAsync();
    }

    private void OpenAddPopup()
    {
        isAddPopupOpen = true;
    }

    private void CloseAddPopup()
    {
        isAddPopupOpen = false;
    }

    private async Task AddGame(Game game)
    {
        DbContext.Games.Add(game);
        await DbContext.SaveChangesAsync();

        games.Add(game);
        CloseAddPopup();
    }

    private async Task HandleRemove(Game game)
    {
        DbContext.Games.Remove(game);
        await DbContext.SaveChangesAsync();

        games.Remove(game);
    }
}
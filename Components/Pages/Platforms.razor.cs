using GameLogBook.Data;
using GameLogBook.Models.Platforms;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Platforms : ComponentBase
{
    [Inject]
    private GameLogBookDbContext DbContext { get; set; } = null!;
    
    private List<Platform> platforms = new();

    private bool isAddPopupOpen;
    
    protected override async Task OnInitializedAsync()
    {
        platforms = await DbContext.Platforms.ToListAsync();
    }

    private void OpenAddPopup()
    {
        isAddPopupOpen = true;
    }

    private void CloseAddPopup()
    {
        isAddPopupOpen = false;
    }
}
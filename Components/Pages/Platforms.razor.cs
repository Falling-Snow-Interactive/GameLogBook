using GameLogBook.Models.Games;
using GameLogBook.Models.Platforms;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Platforms : CollectionPageBase<Platform>
{
    private List<Game> games = [];

    protected override DbSet<Platform> EntitySet => DbContext.Platforms;

    protected override string GetSortKey(Platform item)
    {
        return item.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        games = await DbContext.Games
                               .OrderBy(game => game.Name)
                               .ToListAsync();
    }

    private async Task AddPlatform(Platform platform)
    {
        await AddItemAsync(platform);
        CloseAddPopup();
    }

    private async Task RemovePlatform(Platform platform)
    {
        await RemoveItemAsync(platform);
    }
}

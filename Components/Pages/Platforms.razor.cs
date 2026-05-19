using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using GameLogBook.Models.Platforms;
using Microsoft.EntityFrameworkCore;
using PlatformModel = GameLogBook.Models.Platforms.Platform;

namespace GameLogBook.Components.Pages;

public partial class Platforms : CollectionPageBase<PlatformModel>
{
    private List<Game> games = [];
    private List<Company> companies = [];

    protected override DbSet<PlatformModel> EntitySet => DbContext.Platforms;

    protected override string GetSortKey(PlatformModel item)
    {
        return item.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        games = await DbContext.Games
                               .OrderBy(game => game.Name)
                               .ToListAsync();

        companies = await DbContext.Companies
                                   .OrderBy(company => company.Name)
                                   .ToListAsync();
    }

    private async Task AddPlatform(PlatformModel platform)
    {
        await AddItemAsync(platform);
        CloseAddPopup();
    }

    private async Task RemovePlatform(PlatformModel platform)
    {
        await RemoveItemAsync(platform);
    }
}

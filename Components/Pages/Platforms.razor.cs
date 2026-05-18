using GameLogBook.Models.Platforms;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Platforms : CollectionPageBase<Platform>
{
    protected override DbSet<Platform> EntitySet => DbContext.Platforms;

    protected override string GetSortKey(Platform item)
    {
        return item.Name;
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

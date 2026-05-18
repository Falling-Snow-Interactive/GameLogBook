using GameLogBook.Models.Games;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Games : CollectionPageBase<Game>
{
    protected override DbSet<Game> EntitySet => DbContext.Games;

    protected override string GetSortKey(Game item)
    {
        return item.Name;
    }

    private async Task AddGame(Game game)
    {
        await AddItemAsync(game);
        CloseAddPopup();
    }

    private async Task RemoveGame(Game game)
    {
        await RemoveItemAsync(game);
    }
}

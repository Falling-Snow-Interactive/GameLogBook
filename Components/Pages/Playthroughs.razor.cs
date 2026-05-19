using GameLogBook.Models;
using GameLogBook.Models.Games;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Playthroughs : CollectionPageBase<Playthrough>
{
    public IReadOnlyList<Game> Games { get; set; } = [];
    private Playthrough? selectedPlaythrough;

    protected override DbSet<Playthrough> EntitySet => DbContext.Playthroughs;

    protected override string GetSortKey(Playthrough item)
    {
        return item.Name;
    }
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Games = await DbContext.Games
                               .OrderBy(game => game.Name)
                               .ToListAsync();
    }

    private async Task AddPlaythrough(Playthrough playthrough)
    {
        await AddItemAsync(playthrough);
        CloseAddPopup();
    }

    private async Task UpdatePlaythrough(Playthrough updatedPlaythrough)
    {
        if (selectedPlaythrough is null)
        {
            return;
        }

        Playthrough? existingPlaythrough = await DbContext.Playthroughs
                                                          .FirstOrDefaultAsync(playthrough => playthrough.ID == selectedPlaythrough.ID);

        if (existingPlaythrough is null)
        {
            CloseEditPopup();
            return;
        }

        existingPlaythrough.Name = updatedPlaythrough.Name.Trim();
        existingPlaythrough.GameIds = updatedPlaythrough.GameIds.Distinct().ToArray();

        await UpdateItemAsync();
        CloseEditPopup();
    }

    private async Task RemovePlaythrough(Playthrough playthrough)
    {
        await RemoveItemAsync(playthrough);
    }

    private void OpenEditPopup(Playthrough playthrough)
    {
        selectedPlaythrough = new Playthrough
                              {
                                  ID = playthrough.ID,
                                  Name = playthrough.Name,
                                  GameIds = playthrough.GameIds.ToArray()
                              };
    }

    private void CloseEditPopup()
    {
        selectedPlaythrough = null;
    }

    private static string GetPlaythroughSummary(Playthrough playthrough)
    {
        return playthrough.GameIds.Length switch
        {
            0 => "No linked games yet",
            1 => "1 linked game",
            _ => $"{playthrough.GameIds.Length} linked games"
        };
    }
}

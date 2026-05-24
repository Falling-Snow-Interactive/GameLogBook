using GameLogBook.Components.Elements.AddPlaythrough;
using GameLogBook.Models;
using GameLogBook.Models.Games;
using GameLogBook.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Playthroughs : CollectionPageBase<Playthrough>
{
    public IReadOnlyList<Game> Games { get; set; } = [];

    [Inject]
    private PopupService PopupService { get; set; } = null!;

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
    }

    private async Task UpdatePlaythrough(Playthrough updatedPlaythrough)
    {
        Playthrough? existingPlaythrough = await DbContext.Playthroughs
                                                          .FirstOrDefaultAsync(playthrough => playthrough.ID == updatedPlaythrough.ID);

        if (existingPlaythrough is null)
        {
            return;
        }

        existingPlaythrough.Name = updatedPlaythrough.Name.Trim();
        existingPlaythrough.GameIds = updatedPlaythrough.GameIds.Distinct().ToArray();

        await UpdateItemAsync();
    }

    private async Task RemovePlaythrough(Playthrough playthrough)
    {
        await RemoveItemAsync(playthrough);
    }

    protected override async Task OpenAddPopup()
    {
        Playthrough? playthrough = await PopupService.ShowAsync<AddPlaythroughPopup, Playthrough>(
            new Dictionary<string, object?>
            {
                [nameof(AddPlaythroughPopup.LibraryGames)] = Games
            });

        if (playthrough is not null)
        {
            await AddPlaythrough(playthrough);
        }
    }

    private async Task OpenEditPopup(Playthrough playthrough)
    {
        Playthrough editablePlaythrough = new()
                                          {
                                              ID = playthrough.ID,
                                              Name = playthrough.Name,
                                              GameIds = playthrough.GameIds.ToArray()
                                          };

        Playthrough? updatedPlaythrough = await PopupService.ShowAsync<AddPlaythroughPopup, Playthrough>(
            new Dictionary<string, object?>
            {
                [nameof(AddPlaythroughPopup.InitialPlaythrough)] = editablePlaythrough,
                [nameof(AddPlaythroughPopup.LibraryGames)] = Games
            });

        if (updatedPlaythrough is not null)
        {
            await UpdatePlaythrough(updatedPlaythrough);
        }
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

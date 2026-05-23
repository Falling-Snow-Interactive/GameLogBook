using GameLogBook.Models.Games;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Games : CollectionPageBase<Game>
{
    private List<GameLogBook.Models.Companies.Company> companies = [];
    private Game? selectedGame;

    protected override DbSet<Game> EntitySet => DbContext.Games;

    protected override string GetSortKey(Game item)
    {
        return item.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadCompaniesAsync();
    }

    private async Task AddGame(Game game)
    {
        game.DeveloperCompanyIds = NormalizeCompanyIds(game.DeveloperCompanyIds);
        game.PublisherCompanyIds = NormalizeCompanyIds(game.PublisherCompanyIds);

        await AddItemAsync(game);
        await LoadItemsAsync();
        CloseAddPopup();
    }

    private async Task UpdateGame(Game updatedGame)
    {
        if (selectedGame is null)
        {
            return;
        }

        Game? existingGame = await BuildQuery()
                                  .FirstOrDefaultAsync(game => game.Id == selectedGame.Id);

        if (existingGame is null)
        {
            CloseEditPopup();
            return;
        }

        existingGame.IgdbId = updatedGame.IgdbId;
        existingGame.Name = updatedGame.Name.Trim();
        existingGame.ReleaseDate = updatedGame.ReleaseDate;
        existingGame.Summary = string.IsNullOrWhiteSpace(updatedGame.Summary) ? null : updatedGame.Summary.Trim();
        existingGame.Cover = string.IsNullOrWhiteSpace(updatedGame.Cover?.ImagePath)
                                 ? null
                                 : new GameLogBook.Models.Games.Cover
                                   {
                                       ImagePath = updatedGame.Cover.ImagePath.Trim()
                                   };
        existingGame.DeveloperCompanyIds = NormalizeCompanyIds(updatedGame.DeveloperCompanyIds);
        existingGame.PublisherCompanyIds = NormalizeCompanyIds(updatedGame.PublisherCompanyIds);

        await UpdateItemAsync();
        CloseEditPopup();
    }

    private async Task RemoveGame(Game game)
    {
        await RemoveItemAsync(game);
    }

    private async Task LoadCompaniesAsync()
    {
        companies = await DbContext.Companies
                                   .OrderBy(company => company.Name)
                                   .ToListAsync();
    }

    private void OpenEditPopup(Game game)
    {
        selectedGame = CloneGame(game);
    }

    private void CloseEditPopup()
    {
        selectedGame = null;
    }

    private static Game CloneGame(Game game)
    {
        return new Game
               {
                   Id = game.Id,
                   IgdbId = game.IgdbId,
                   Name = game.Name,
                   Summary = game.Summary,
                   ReleaseDate = game.ReleaseDate,
                   Cover = game.Cover is null
                               ? null
                               : new GameLogBook.Models.Games.Cover
                                 {
                                     ImagePath = game.Cover.ImagePath
                                 },
                   DeveloperCompanyIds = game.DeveloperCompanyIds.ToArray(),
                   PublisherCompanyIds = game.PublisherCompanyIds.ToArray()
               };
    }

    private static int[] NormalizeCompanyIds(IEnumerable<int> companyIds)
    {
        return companyIds
               .Where(companyId => companyId > 0)
               .Distinct()
               .Order()
               .ToArray();
    }
}

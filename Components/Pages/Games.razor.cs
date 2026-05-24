using GameLogBook.Models.Games;
using GameLogBook.Models.Games.Company;
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

    protected override IQueryable<Game> BuildQuery()
    {
        return EntitySet.Include(game => game.GameCompanies);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadCompaniesAsync();
    }

    private async Task AddGame(Game game)
    {
        game.GameCompanies = NormalizeCompanyIds(game.GameCompanies);

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
                                 .FirstOrDefaultAsync(game => game.ID == selectedGame.ID);

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
        existingGame.GameCompanies = NormalizeCompanyIds(updatedGame.GameCompanies);

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

    private void OnClickGame(Game game)
    {
        selectedGame = CloneGame(game);
    }

    private void CloseEditPopup()
    {
        selectedGame = null;
    }

    private static Game CloneGame(Game game)
    {
        return new Game(game);
    }

    private static List<GameCompany> NormalizeCompanyIds(IEnumerable<GameCompany> companies)
    {
        return companies
               .Where(company => company.CompanyID > 0)
               .Distinct()
               .Order()
               .ToList();
    }

    private void HandleGameViewClose()
    {
        selectedGame = null;
    }

    private void HandleGameViewEdit()
    {
        
    }
}

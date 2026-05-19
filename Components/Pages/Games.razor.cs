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

    protected override IQueryable<Game> BuildQuery()
    {
        return EntitySet
               .Include(game => game.Companies)
               .ThenInclude(gameCompany => gameCompany.Company);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadCompaniesAsync();
    }

    private async Task AddGame(Game game)
    {
        await PopulateCompaniesAsync(game);

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
        existingGame.Cover = string.IsNullOrWhiteSpace(updatedGame.Cover?.Url)
                                 ? null
                                 : new GameLogBook.Models.Games.Cover
                                   {
                                       Url = updatedGame.Cover.Url.Trim()
                                   };

        existingGame.Companies.Clear();

        foreach (GameCompany gameCompany in updatedGame.Companies)
        {
            existingGame.Companies.Add(new GameCompany
                                       {
                                           Company = gameCompany.Company,
                                           Role = gameCompany.Role
                                       });
        }

        await PopulateCompaniesAsync(existingGame);

        await UpdateItemAsync();
        await LoadCompaniesAsync();
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

    private async Task PopulateCompaniesAsync(Game game)
    {
        List<GameCompany> selectedCompanies = game.Companies.ToList();
        game.Companies.Clear();

        foreach (GameCompany selectedCompany in selectedCompanies)
        {
            GameLogBook.Models.Companies.Company company = await GetOrCreateCompany(selectedCompany.Company);

            if (game.Companies.Any(gameCompany => gameCompany.Company == company
                                                  && gameCompany.Role == selectedCompany.Role))
            {
                continue;
            }

            game.Companies.Add(new GameCompany
                               {
                                   Company = company,
                                   Role = selectedCompany.Role
                               });
        }
    }

    private async Task<GameLogBook.Models.Companies.Company> GetOrCreateCompany(GameLogBook.Models.Companies.Company selectedCompany)
    {
        GameLogBook.Models.Companies.Company? company = null;

        if (selectedCompany.Id > 0)
        {
            company = await DbContext.Companies
                                     .FirstOrDefaultAsync(existingCompany => existingCompany.Id == selectedCompany.Id);
        }

        if (company is null && selectedCompany.IgdbId.HasValue)
        {
            company = await DbContext.Companies
                                     .FirstOrDefaultAsync(existingCompany =>
                                                              existingCompany.IgdbId == selectedCompany.IgdbId.Value);
        }

        company ??= await DbContext.Companies
                                   .FirstOrDefaultAsync(existingCompany =>
                                                            existingCompany.IgdbId == null
                                                            && existingCompany.Name == selectedCompany.Name);

        if (company is null)
        {
            company = new GameLogBook.Models.Companies.Company();
            DbContext.Companies.Add(company);
        }

        company.IgdbId = selectedCompany.IgdbId;
        company.Name = selectedCompany.Name.Trim();
        company.CoverUrl = string.IsNullOrWhiteSpace(selectedCompany.CoverUrl)
                               ? company.CoverUrl
                               : selectedCompany.CoverUrl.Trim();
        company.LastSyncedAt = selectedCompany.LastSyncedAt ?? DateTimeOffset.UtcNow;

        return company;
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
                                     Url = game.Cover.Url
                                 },
                   Companies = game.Companies
                                   .Select(gameCompany => new GameCompany
                                                          {
                                                              GameId = gameCompany.GameId,
                                                              CompanyId = gameCompany.CompanyId,
                                                              Role = gameCompany.Role,
                                                              Company = new GameLogBook.Models.Companies.Company
                                                                        {
                                                                            Id = gameCompany.Company.Id,
                                                                            IgdbId = gameCompany.Company.IgdbId,
                                                                            Name = gameCompany.Company.Name,
                                                                            CoverUrl = gameCompany.Company.CoverUrl,
                                                                            LastSyncedAt = gameCompany.Company.LastSyncedAt
                                                                        }
                                                          })
                                   .ToList()
               };
    }
}

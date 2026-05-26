using System.Diagnostics.CodeAnalysis;
using VGL.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Elements.GameElements;
using VGL.Components.Popups;
using VGL.Models.Games;
using VGL.Models.Games.Company;
using VGL.Services;
using Company = VGL.Models.Companies.Company;
using GameView = VGL.Components.Elements.GameElements.GameView;

namespace VGL.Components.Pages;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class GamesPage : CollectionPageBase<Game>
{
    private List<Company> companies = [];

    [Inject]
    private PopupService PopupService { get; set; } = null!;

    protected override DbSet<Game> EntitySet => DbContext.Games;

    protected override string GetSortKey(Game item)
    {
        return item.Name;
    }

    protected override IQueryable<Game> BuildQuery()
    {
        return EntitySet.Include(game => game.GameCompanies)
                        .ThenInclude(gameCompany => gameCompany.Company);
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
    }

    private async Task<Company?> AddCompanyFromSearch(Company newCompany)
    {
        Company? existingCompany = null;

        if (newCompany.IGDB.HasValue)
        {
            existingCompany = await DbContext.Companies.FirstOrDefaultAsync(company => company.IGDB == newCompany.IGDB.Value);
        }

        if (existingCompany is null && !string.IsNullOrWhiteSpace(newCompany.Name))
        {
            string trimmedName = newCompany.Name.Trim();

            existingCompany = await DbContext.Companies
                                             .FirstOrDefaultAsync(company => company.IGDB == null
                                                                             && company.Name == trimmedName);
        }

        if (existingCompany is not null)
        {
            existingCompany.Name = newCompany.Name.Trim();
            existingCompany.ImagePath = string.IsNullOrWhiteSpace(newCompany.ImagePath)
                                            ? existingCompany.ImagePath
                                            : newCompany.ImagePath.Trim();
            existingCompany.LastSyncedAt = DateTimeOffset.UtcNow;

            await DbContext.SaveChangesAsync();
            await LoadCompaniesAsync();
            return existingCompany;
        }

        newCompany.Name = newCompany.Name.Trim();
        newCompany.ImagePath = string.IsNullOrWhiteSpace(newCompany.ImagePath)
                                   ? null
                                   : newCompany.ImagePath.Trim();
        newCompany.LastSyncedAt = DateTimeOffset.UtcNow;

        DbContext.Companies.Add(newCompany);
        await DbContext.SaveChangesAsync();
        await LoadCompaniesAsync();

        return newCompany;
    }

    private async Task UpdateGame(Game updatedGame)
    {
        Game? existingGame = await BuildQuery()
                                 .FirstOrDefaultAsync(game => game.ID == updatedGame.ID);

        if (existingGame is null)
        {
            return;
        }

        existingGame.IGDB = updatedGame.IGDB;
        existingGame.Name = updatedGame.Name.Trim();
        existingGame.ReleaseDate = updatedGame.ReleaseDate;
        existingGame.Summary = string.IsNullOrWhiteSpace(updatedGame.Summary) ? null : updatedGame.Summary.Trim();
        existingGame.Cover = string.IsNullOrWhiteSpace(updatedGame.Cover?.Path)
                                 ? null
                                 : new ImageRef
                                   {
                                       Path = updatedGame.Cover.Path.Trim()
                                   };
        existingGame.Hero = string.IsNullOrWhiteSpace(updatedGame.Hero?.Path)
                                ? null
                                : new ImageRef
                                  {
                                      Path = updatedGame.Hero.Path.Trim()
                                  };
        existingGame.Logo = string.IsNullOrWhiteSpace(updatedGame.Logo?.Path)
                                ? null
                                : new ImageRef
                                  {
                                      Path = updatedGame.Logo.Path.Trim()
                                  };
        existingGame.Icon = string.IsNullOrWhiteSpace(updatedGame.Icon?.Path)
                                ? null
                                : new ImageRef
                                  {
                                      Path = updatedGame.Icon.Path.Trim()
                                  };
        existingGame.GameCompanies = NormalizeCompanyIds(updatedGame.GameCompanies);

        await UpdateItemAsync();
    }

    private async Task RemoveGame(Game game)
    {
        await RemoveItemAsync(game);
    }

    private List<Company> GetRelatedCompanies(Game game)
    {
        HashSet<int> companyIds = game.GetAllCompanyIDs().ToHashSet();

        return companies
               .Where(company => companyIds.Contains(company.ID))
               .OrderBy(company => company.Name)
               .ToList();
    }

    private async Task LoadCompaniesAsync()
    {
        companies = await DbContext.Companies
                                   .OrderBy(company => company.Name)
                                   .ToListAsync();
    }

    protected override async Task OpenAddPopup()
    {
        Game? game = await PopupService.ShowAsync<AddGamePopup, Game>(
                                                                      new Dictionary<string, object?>
                                                                      {
                                                                          [nameof(AddGamePopup.Companies)] = companies,
                                                                          [nameof(AddGamePopup.OnCompanyAdded)] = new Func<Company, Task<Company?>>(AddCompanyFromSearch)
                                                                      });

        if (game is not null)
        {
            await AddGame(game);
        }
    }

    private async Task OnClickGame(Game game)
    {
        Game selectedGame = new(game);
        bool? shouldEdit = await PopupService.ShowAsync<GameView, bool>(
                                                                        new Dictionary<string, object?>
                                                                        {
                                                                            [nameof(GameView.Game)] = selectedGame
                                                                        });

        if (shouldEdit == true)
        {
            await OpenEditPopup(selectedGame);
        }
    }

    private static List<GameCompany> NormalizeCompanyIds(IEnumerable<GameCompany> companies)
    {
        return companies
               .Where(company => company.CompanyID > 0)
               .GroupBy(company => new
                                   {
                                       company.CompanyID,
                                       company.Role,
                                   })
               .Select(group => group.First())
               .OrderBy(company => company.Role)
               .ThenBy(company => company.CompanyID)
               .ToList();
    }

    private async Task OpenEditPopup(Game game)
    {
        Game? updatedGame = await PopupService.ShowAsync<AddGamePopup, Game>(new Dictionary<string, object?>
                                                                             {
                                                                                 [nameof(AddGamePopup.InitialGame)] = game,
                                                                                 [nameof(AddGamePopup.Companies)] = companies,
                                                                                 [nameof(AddGamePopup.OnCompanyAdded)] = new Func<Company, Task<Company?>>(AddCompanyFromSearch)
                                                                             });

        if (updatedGame is not null)
        {
            await UpdateGame(updatedGame);
        }
    }
}

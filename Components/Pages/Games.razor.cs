using GameLogBook.Models.Games;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Games : CollectionPageBase<Game>
{
    private List<GameLogBook.Models.Companies.Company> companies = [];

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

        await AddItemAsync(game);
        CloseAddPopup();
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
}

using GameLogBook.Data;
using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Companies
{
    [Inject]
    private GameLogBookDbContext DbContext { get; set; } = null!;

    private List<Company> companies = [];
    private List<Game> games = [];
    private HashSet<int> selectedGameIds = [];

    private bool isAddPopupOpen;
    private string newCompanyName = string.Empty;
    private string newCompanyCoverUrl = string.Empty;
    private bool newCompanyIsPublisher;
    private bool newCompanyIsDeveloper;

    protected override async Task OnInitializedAsync()
    {
        companies = await DbContext.Companies
                                   .OrderBy(company => company.Name)
                                   .ToListAsync();
        games = await DbContext.Games
                               .OrderBy(game => game.Name)
                               .ToListAsync();
    }

    private void OpenAddPopup()
    {
        isAddPopupOpen = true;
    }

    private void CloseAddPopup()
    {
        isAddPopupOpen = false;
        ResetForm();
    }

    private async Task AddCompany()
    {
        Company company = new()
                          {
                              Name = newCompanyName.Trim(),
                              CoverUrl = string.IsNullOrWhiteSpace(newCompanyCoverUrl)
                                             ? null
                                             : newCompanyCoverUrl.Trim(),
                              IsDeveloper = newCompanyIsDeveloper,
                              IsPublisher = newCompanyIsPublisher,
                              GameIds = selectedGameIds
                                        .OrderBy(gameId => gameId)
                                        .ToArray()
                          };

        DbContext.Companies.Add(company);
        await DbContext.SaveChangesAsync();

        companies.Add(company);
        companies = companies
                    .OrderBy(existingCompany => existingCompany.Name)
                    .ToList();
        CloseAddPopup();
    }

    private async Task HandleRemove(Company company)
    {
        DbContext.Companies.Remove(company);
        await DbContext.SaveChangesAsync();

        companies.Remove(company);
    }

    private string GetGameName(int gameId)
    {
        return games.FirstOrDefault(game => game.Id == gameId)?.Name ?? $"Game #{gameId}";
    }

    private void ToggleGameSelection(int gameId, ChangeEventArgs args)
    {
        if (args.Value is true)
        {
            selectedGameIds.Add(gameId);
            return;
        }

        selectedGameIds.Remove(gameId);
    }

    private void ResetForm()
    {
        newCompanyName = string.Empty;
        newCompanyCoverUrl = string.Empty;
        newCompanyIsPublisher = false;
        newCompanyIsDeveloper = false;
        selectedGameIds.Clear();
    }
}

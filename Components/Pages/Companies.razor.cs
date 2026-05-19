using GameLogBook.Data;
using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Components.Pages;

public partial class Companies : CollectionPageBase<Company>
{
    private List<Game> games = [];
    private HashSet<int> selectedGameIds = [];

    private string newCompanyName = string.Empty;
    private string newCompanyCoverUrl = string.Empty;
    private bool newCompanyIsPublisher;
    private bool newCompanyIsDeveloper;

    protected override DbSet<Company> EntitySet => DbContext.Companies;

    protected override string GetSortKey(Company item)
    {
        return item.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        games = await DbContext.Games
                               .OrderBy(game => game.Name)
                               .ToListAsync();
    }

    protected override void CloseAddPopup()
    {
        base.CloseAddPopup();
        ResetForm();
    }

    private void HandleCompanySelected(Company company)
    {
        newCompanyName = company.Name;
        newCompanyCoverUrl = company.CoverUrl ?? string.Empty;
        newCompanyIsDeveloper = company.IsDeveloper;
        newCompanyIsPublisher = company.IsPublisher;
        selectedGameIds = company.GameIds.ToHashSet();
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

        await AddItemAsync(company);
        CloseAddPopup();
    }

    private async Task HandleRemove(Company company)
    {
        await RemoveItemAsync(company);
    }

    private string GetGameName(int gameId)
    {
        return games.FirstOrDefault(game => game.Id == gameId)?.Name ?? $"Game #{gameId}";
    }

    private static string GetCompanyRoleSummary(Company company)
    {
        if (company.IsDeveloper && company.IsPublisher)
        {
            return "Developer · Publisher";
        }

        if (company.IsDeveloper)
        {
            return "Developer";
        }

        if (company.IsPublisher)
        {
            return "Publisher";
        }

        return "No matching local games yet";
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

    public bool TryGetLocalCompany(long? igdbId, out Company? company)
    {
        if (!igdbId.HasValue)
        {
            company = null;
            return false;
        }

        company = DbContext.Companies.FirstOrDefault(company => company.IgdbId == igdbId.Value);
        return company != null;
    }
}

using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.GameElements;

public partial class GameEntry
{
    [Parameter]
    public Game Game { get; set; } = null!;

    [Parameter]
    public IReadOnlyList<Company> Companies { get; set; } = [];

    [Parameter]
    public EventCallback<Game> OnClick { get; set; }
    
    [Parameter]
    public EventCallback OnPlaythroughs { get; set; }
    
    [Parameter]
    public EventCallback OnLogs { get; set; }
    
    [Parameter]
    public EventCallback<Game> OnRemove { get; set; }

    private IReadOnlyList<string> DeveloperNames => GetCompanyNames(Game.GetDeveloperIDs());

    private IReadOnlyList<string> PublisherNames => GetCompanyNames(Game.GetPublisherIDs());

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync(Game);
    }

    private async Task HandlePlaythroughs()
    {
        await OnPlaythroughs.InvokeAsync();
    }

    private async Task HandleLogs()
    {
        await OnLogs.InvokeAsync();
    }

    private async Task HandleRemove()
    {
        await OnRemove.InvokeAsync(Game);
    }

    private IReadOnlyList<string> GetCompanyNames(IEnumerable<int> companyIds)
    {
        return Companies
               .Where(company => companyIds.Contains(company.ID))
               .Select(company => company.Name)
               .Where(name => !string.IsNullOrWhiteSpace(name))
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .OrderBy(name => name)
               .ToList();
    }
}

using GameLogBook.Models.Games;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Components.Elements.GameEntry;

public partial class GameEntry
{
    [Parameter]
    public Game Game { get; set; } = null!;

    [Parameter]
    public bool ShowButtons { get; set; } = false;

    [Parameter]
    public EventCallback<Game> OnClick { get; set; }
    
    [Parameter]
    public EventCallback OnPlaythroughs { get; set; }
    
    [Parameter]
    public EventCallback OnLogs { get; set; }
    
    [Parameter]
    public EventCallback<Game> OnRemove { get; set; }

    private IReadOnlyList<string> DeveloperNames => GetCompanyNames(GameCompanyRole.Developer);

    private IReadOnlyList<string> PublisherNames => GetCompanyNames(GameCompanyRole.Publisher);

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

    private IReadOnlyList<string> GetCompanyNames(GameCompanyRole role)
    {
        return Game.Companies
                   .Where(gameCompany => gameCompany.Role == role)
                   .Select(gameCompany => gameCompany.Company.Name)
                   .Where(name => !string.IsNullOrWhiteSpace(name))
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .OrderBy(name => name)
                   .ToList();
    }
}

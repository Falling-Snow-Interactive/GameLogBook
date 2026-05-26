using Microsoft.AspNetCore.Components;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Models.Games.Company;
using VGL.Models.Libraries.Entries;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Elements.LibraryEntries;

public partial class LibraryEntry : ComponentBase
{
    [Parameter]
    public ILibraryEntry Entry { get; set; } = null!;
    
    [Parameter]
    public List<Game>? RelatedGames { get; set; }
    
    [Parameter]
    public List<Company>? RelatedCompanies { get; set; }

    [Parameter]
    public EventCallback<ILibraryEntry> OnClick { get; set; }
    
    [Parameter]
    public EventCallback<ILibraryEntry> OnRemove { get; set; }

    private IReadOnlyList<Company> DeveloperCompanies => GetRoleCompanies(GameCompanyRole.Developer);

    private IReadOnlyList<Company> PublisherCompanies => GetRoleCompanies(GameCompanyRole.Publisher);

    private bool HasCompanyIcons => DeveloperCompanies.Count > 0
                                    || PublisherCompanies.Count > 0;

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync(Entry);
    }

    private async Task HandleRemove()
    {
        await OnRemove.InvokeAsync(Entry);
    }

    private IReadOnlyList<Company> GetRoleCompanies(GameCompanyRole role)
    {
        List<int> companyIds = GetCompanyIds(role);

        if (companyIds.Count == 0)
        {
            return [];
        }

        HashSet<int> companyIdSet = companyIds.ToHashSet();

        if (RelatedCompanies is { Count: > 0 })
        {
            return RelatedCompanies
                   .Where(company => companyIdSet.Contains(company.ID))
                   .DistinctBy(company => company.ID)
                   .OrderBy(company => company.Name)
                   .ToList();
        }

        if (Entry is Game game)
        {
            return game.GameCompanies
                       .Where(gameCompany => gameCompany.Role == role)
                       .Select(gameCompany => gameCompany.Company)
                       .Where(company => company is not null)
                       .DistinctBy(company => company.ID)
                       .OrderBy(company => company.Name)
                       .ToList();
        }

        return [];
    }

    private List<int> GetCompanyIds(GameCompanyRole role)
    {
        return Entry switch
               {
                   Game game when role is GameCompanyRole.Developer => game.GetDeveloperIDs(),
                   Game game when role is GameCompanyRole.Publisher => game.GetPublisherIDs(),
                   PlatformModel platform when role is GameCompanyRole.Developer => platform.GetDeveloperIDs(),
                   _ => [],
               };
    }

    private static string? GetCompanyImagePath(Company company)
    {
        return company.Icon?.Path
               ?? company.Logo?.Path
               ?? company.ImagePath
               ?? company.Cover?.Path;
    }

    private static string GetCompanyInitials(Company company)
    {
        string[] words = company.Name
                                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length == 0)
        {
            return "?";
        }

        return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
    }
}

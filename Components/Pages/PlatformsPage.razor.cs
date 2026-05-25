using GameLogBook.Components.Elements.AddPlatform;
using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using GameLogBook.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PlatformModel = GameLogBook.Models.Platforms.Platform;

namespace GameLogBook.Components.Pages;

public partial class PlatformsPage : CollectionPageBase<PlatformModel>
{
    private List<Game> games = [];
    private List<Company> companies = [];

    [Inject]
    private PopupService PopupService { get; set; } = null!;

    protected override DbSet<PlatformModel> EntitySet => DbContext.Platforms;
    
    private Dictionary<int, List<string>> gameNamesByPlatformID = [];

    protected override string GetSortKey(PlatformModel item)
    {
        return item.Name;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        games = await DbContext.Games
                               .AsNoTracking()
                               .OrderBy(game => game.Name)
                               .ToListAsync();

        companies = await DbContext.Companies
                                   .AsNoTracking()
                                   .OrderBy(company => company.Name)
                                   .ToListAsync();
    }

    // private static string GetLinkedGameSummary(PlatformModel platform)
    // {
    //     var games = gameNames
    //     int linkedGameCount = platform.GameIds?.Length ?? 0;
    //     return linkedGameCount == 0
    //                ? "No linked games"
    //                : $"{linkedGameCount} linked game{(linkedGameCount == 1 ? string.Empty : "s")}";
    // }

    private async Task AddPlatform(PlatformModel platform)
    {
        await AddItemAsync(platform);
    }

    private async Task UpdatePlatform(PlatformModel updatedPlatform)
    {
        PlatformModel? existingPlatform = await DbContext.Platforms
                                                         .FirstOrDefaultAsync(platform => platform.ID == updatedPlatform.ID);

        if (existingPlatform is null)
        {
            return;
        }

        existingPlatform.IgdbId = updatedPlatform.IgdbId;
        existingPlatform.Name = updatedPlatform.Name.Trim();
        existingPlatform.Abbreviation = updatedPlatform.Abbreviation.Trim();
        existingPlatform.ImagePath = string.IsNullOrWhiteSpace(updatedPlatform.ImagePath)
                                         ? null
                                         : updatedPlatform.ImagePath.Trim();
        existingPlatform.ReleaseDate = updatedPlatform.ReleaseDate;
        existingPlatform.ManufacturerIds = updatedPlatform.ManufacturerIds ?? [];
        // existingPlatform.GameIds = updatedPlatform.GameIds ?? [];

        await UpdateItemAsync();
    }

    private async Task RemovePlatform(PlatformModel platform)
    {
        await RemoveItemAsync(platform);
    }

    protected override async Task OpenAddPopup()
    {
        PlatformModel? platform = await PopupService.ShowAsync<AddPlatformPopup, PlatformModel>(new Dictionary<string, object?>
                                                                                                {
                                                                                                    [nameof(AddPlatformPopup.Games)] = games,
                                                                                                    [nameof(AddPlatformPopup.Companies)] = companies
                                                                                                });

        if (platform is not null)
        {
            await AddPlatform(platform);
        }
    }

    private async Task OpenEditPopup(PlatformModel platform)
    {
        PlatformModel? updatedPlatform = await PopupService.ShowAsync<AddPlatformPopup, PlatformModel>(
            new Dictionary<string, object?>
            {
                [nameof(AddPlatformPopup.InitialPlatform)] = new PlatformModel(platform),
                [nameof(AddPlatformPopup.Games)] = games,
                [nameof(AddPlatformPopup.Companies)] = companies
            });

        if (updatedPlatform is not null)
        {
            await UpdatePlatform(updatedPlatform);
        }
    }

    private List<string> GetGameNames(PlatformModel platform)
    {
        return gameNamesByPlatformID.GetValueOrDefault(platform.ID) ?? [];
    }
    
    private bool PlatformHasLinkedGames(PlatformModel platform)
    {
        return gameNamesByPlatformID.ContainsKey(platform.ID);
    }
    
    private async Task LoadGamePlatformSummaries()
    {
        games = await DbContext.Games
                               .Include(game => game.GamePlatforms)
                               .OrderBy(game => game.Name)
                               .ToListAsync();

        gameNamesByPlatformID = games
                                .SelectMany(game => game.GetDeveloperIDs()
                                                        .Concat(game.GetPublisherIDs())
                                                        .Distinct()
                                                        .Select(companyId => new
                                                                             {
                                                                                 game.Name,
                                                                                 CompanyId = companyId
                                                                             }))
                                .GroupBy(item => item.CompanyId)
                                .ToDictionary(group => group.Key,
                                              group => group.Select(item => item.Name)
                                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                                            .OrderBy(name => name)
                                                            .ToList());
    }
}

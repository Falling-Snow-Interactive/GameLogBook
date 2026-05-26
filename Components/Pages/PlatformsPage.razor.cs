using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Elements.PlatformElements;
using VGL.Components.Popups;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Services;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Pages;

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
        
        existingPlatform.Name = updatedPlatform.Name.Trim();
        existingPlatform.ShortName = updatedPlatform.ShortName?.Trim();
        existingPlatform.ReleaseDate = updatedPlatform.ReleaseDate;
        existingPlatform.Summary = string.IsNullOrWhiteSpace(updatedPlatform.Summary) ? null : updatedPlatform.Summary.Trim();
        
        existingPlatform.Cover = string.IsNullOrWhiteSpace(updatedPlatform.Cover?.Path)
                                     ? null
                                     : new ImageRef
                                       {
                                           Path = updatedPlatform.Cover.Path.Trim()
                                       };
        existingPlatform.Hero = string.IsNullOrWhiteSpace(updatedPlatform.Hero?.Path)
                                    ? null
                                    : new ImageRef
                                      {
                                          Path = updatedPlatform.Hero.Path.Trim()
                                      };
        existingPlatform.Logo = string.IsNullOrWhiteSpace(updatedPlatform.Logo?.Path)
                                    ? null
                                    : new ImageRef
                                      {
                                          Path = updatedPlatform.Logo.Path.Trim()
                                      };
        existingPlatform.Icon = string.IsNullOrWhiteSpace(updatedPlatform.Icon?.Path)
                                    ? null
                                    : new ImageRef
                                      {
                                          Path = updatedPlatform.Icon.Path.Trim()
                                      };
        
        // TODO - Turn into relation DB refs
        existingPlatform.ManufacturerIds = updatedPlatform.ManufacturerIds ?? [];
        
        // IGDB
        existingPlatform.IGDB = updatedPlatform.IGDB;
        
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
        PlatformModel? updatedPlatform = 
            await PopupService
                .ShowAsync<AddPlatformPopup, PlatformModel>(new Dictionary<string, object?>
                                                            {
                                                                [nameof(AddPlatformPopup.InitialPlatform)] 
                                                                    = new PlatformModel(platform),
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

    private List<Company> GetRelatedCompanies(PlatformModel platform)
    {
        HashSet<int> companyIds = (platform.ManufacturerIds ?? []).ToHashSet();

        return companies
               .Where(company => companyIds.Contains(company.ID))
               .OrderBy(company => company.Name)
               .ToList();
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

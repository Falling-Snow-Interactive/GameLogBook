using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Popups;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Models.Platforms.Company;
using VGL.Models.Users;
using VGL.Services;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Pages;

public partial class PlatformsPage : CollectionPageBase<PlatformModel>
{
    private List<Game> games = [];
    private List<Company> companies = [];
    private Dictionary<int, List<string>> gameNamesByPlatformID = [];

    [Inject]
    private PopupService PopupService { get; set; } = null!;

    protected override DbSet<PlatformModel> EntitySet => DbContext.Platforms;

    protected override string GetSortKey(PlatformModel item)
    {
        return item.Name;
    }

    protected override IQueryable<PlatformModel> BuildQuery()
    {
        return EntitySet.Include(platform => platform.PlatformCompanies)
                        .ThenInclude(platformCompany => platformCompany.Company);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await LoadGamesAsync();
        await LoadCompaniesAsync();
        await LoadGamePlatformSummaries();
    }

    protected override async Task LoadItemsAsync()
    {
        if (UserSession.CurrentUserID is null)
        {
            Items = [];
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;
        Items = await DbContext.UserPlatformCollections
                               .AsNoTracking()
                               .Where(userPlatform => userPlatform.UserProfileID == userProfileId)
                               .Include(userPlatform => userPlatform.Platform)
                               .ThenInclude(platform => platform.PlatformCompanies)
                               .ThenInclude(platformCompany => platformCompany.Company)
                               .Select(userPlatform => userPlatform.Platform)
                               .OrderBy(platform => platform.Name)
                               .ToListAsync();
    }

    private async Task AddPlatform(PlatformModel platform)
    {
        if (UserSession.CurrentUserID is null)
        {
            return;
        }

        platform.PlatformCompanies = NormalizeCompanyIds(platform.PlatformCompanies);

        DbContext.Platforms.Add(platform);
        await DbContext.SaveChangesAsync();

        DbContext.UserPlatformCollections.Add(new UserPlatformCollection
                                              {
                                                  UserProfileID = UserSession.CurrentUserID.Value,
                                                  PlatformID = platform.ID
                                              });

        await DbContext.SaveChangesAsync();
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
            ApplyCompanyDetails(existingCompany, newCompany);

            await DbContext.SaveChangesAsync();
            await LoadCompaniesAsync();
            return existingCompany;
        }

        NormalizeCompanyDetails(newCompany);

        DbContext.Companies.Add(newCompany);
        await DbContext.SaveChangesAsync();
        await LoadCompaniesAsync();

        return newCompany;
    }

    private async Task UpdatePlatform(PlatformModel updatedPlatform)
    {
        PlatformModel? existingPlatform = await BuildQuery()
                                                 .FirstOrDefaultAsync(platform => platform.ID == updatedPlatform.ID);

        if (existingPlatform is null)
        {
            return;
        }

        existingPlatform.Name = updatedPlatform.Name.Trim();
        existingPlatform.ShortName = updatedPlatform.ShortName?.Trim();
        existingPlatform.ReleaseDate = updatedPlatform.ReleaseDate;
        existingPlatform.Summary = string.IsNullOrWhiteSpace(updatedPlatform.Summary) ? null : updatedPlatform.Summary.Trim();
        existingPlatform.Cover = CopyImageRef(updatedPlatform.Cover);
        existingPlatform.Hero = CopyImageRef(updatedPlatform.Hero);
        existingPlatform.Logo = CopyImageRef(updatedPlatform.Logo);
        existingPlatform.Icon = CopyImageRef(updatedPlatform.Icon);
        existingPlatform.AddCompaniesByID(PlatformCompanyRole.Developer, updatedPlatform.GetDeveloperIDs());
        existingPlatform.IGDB = updatedPlatform.IGDB;

        await UpdateItemAsync();
    }

    private async Task RemovePlatform(PlatformModel platform)
    {
        if (UserSession.CurrentUserID is null)
        {
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;
        UserPlatformCollection? userPlatform = await DbContext.UserPlatformCollections
                                                              .FirstOrDefaultAsync(item => item.UserProfileID == userProfileId
                                                                                           && item.PlatformID == platform.ID);

        if (userPlatform is not null)
        {
            DbContext.UserPlatformCollections.Remove(userPlatform);
        }

        List<UserGamePlatformOwnership> ownerships = await DbContext.UserGamePlatformOwnerships
                                                                    .Where(ownership => ownership.UserProfileID == userProfileId
                                                                                        && ownership.PlatformID == platform.ID)
                                                                    .ToListAsync();
        DbContext.UserGamePlatformOwnerships.RemoveRange(ownerships);

        await DbContext.SaveChangesAsync();
        await LoadItemsAsync();
        await LoadGamePlatformSummaries();
    }

    protected override async Task OpenAddPopup()
    {
        PlatformModel? platform = await PopupService.ShowAsync<AddPlatformPopup, PlatformModel>(
            new Dictionary<string, object?>
            {
                [nameof(AddPlatformPopup.Games)] = games,
                [nameof(AddPlatformPopup.Companies)] = companies,
                [nameof(AddPlatformPopup.OnCompanyAdded)] = new Func<Company, Task<Company?>>(AddCompanyFromSearch)
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
                [nameof(AddPlatformPopup.Companies)] = companies,
                [nameof(AddPlatformPopup.OnCompanyAdded)] = new Func<Company, Task<Company?>>(AddCompanyFromSearch)
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
        HashSet<int> companyIds = platform.GetAllCompanyIDs().ToHashSet();

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
        if (UserSession.CurrentUserID is null)
        {
            gameNamesByPlatformID = [];
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;
        List<UserGamePlatformOwnership> ownerships = await DbContext.UserGamePlatformOwnerships
                                                                    .AsNoTracking()
                                                                    .Where(ownership => ownership.UserProfileID == userProfileId)
                                                                    .Include(ownership => ownership.Game)
                                                                    .ToListAsync();

        gameNamesByPlatformID = ownerships
                                .GroupBy(ownership => ownership.PlatformID)
                                .ToDictionary(group => group.Key,
                                              group => group.Select(ownership => ownership.Game.Name)
                                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                                            .OrderBy(name => name)
                                                            .ToList());
    }

    private async Task LoadGamesAsync()
    {
        if (UserSession.CurrentUserID is null)
        {
            games = [];
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;
        games = await DbContext.UserGameCollections
                               .AsNoTracking()
                               .Where(userGame => userGame.UserProfileID == userProfileId)
                               .Include(userGame => userGame.Game)
                               .Select(userGame => userGame.Game)
                               .OrderBy(game => game.Name)
                               .ToListAsync();
    }

    private async Task LoadCompaniesAsync()
    {
        companies = await DbContext.Companies
                                   .OrderBy(company => company.Name)
                                   .ToListAsync();
    }

    private static List<PlatformCompany> NormalizeCompanyIds(IEnumerable<PlatformCompany> companies)
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

    private static void ApplyCompanyDetails(Company target, Company source)
    {
        target.IGDB = source.IGDB ?? target.IGDB;
        target.Name = source.Name.Trim();
        target.Summary = string.IsNullOrWhiteSpace(source.Summary) ? target.Summary : source.Summary.Trim();
        target.FoundedDate = source.FoundedDate ?? target.FoundedDate;
        target.Cover = CopyImageRef(source.Cover) ?? target.Cover;
        target.Hero = CopyImageRef(source.Hero) ?? target.Hero;
        target.Logo = CopyImageRef(source.Logo) ?? target.Logo;
        target.Icon = CopyImageRef(source.Icon) ?? target.Icon;
        target.ImagePath = string.IsNullOrWhiteSpace(source.ImagePath)
                               ? target.ImagePath
                               : source.ImagePath.Trim();
        target.LastSyncedAt = DateTimeOffset.UtcNow;
    }

    private static void NormalizeCompanyDetails(Company company)
    {
        company.Name = company.Name.Trim();
        company.Summary = string.IsNullOrWhiteSpace(company.Summary) ? null : company.Summary.Trim();
        company.ImagePath = string.IsNullOrWhiteSpace(company.ImagePath)
                                ? null
                                : company.ImagePath.Trim();
        company.LastSyncedAt = DateTimeOffset.UtcNow;
    }

    private static ImageRef? CopyImageRef(ImageRef? image)
    {
        return string.IsNullOrWhiteSpace(image?.Path)
                   ? null
                   : new ImageRef
                     {
                         Path = image.Path.Trim()
                     };
    }
}

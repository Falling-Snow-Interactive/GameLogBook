using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Popups;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Models.Games.Company;
using VGL.Models.Games.Platforms;
using VGL.Models.Platforms.Company;
using VGL.Models.Users;
using VGL.Services;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Pages;

public abstract class LogbookPageBase<TEntity> : CollectionPageBase<TEntity>
    where TEntity : class
{
    [Inject]
    protected PopupService PopupService { get; set; } = null!;

    protected async Task<List<Game>> LoadLibraryGamesAsync()
    {
        if (UserSession.CurrentUserID is null)
        {
            return [];
        }

        int userProfileId = UserSession.CurrentUserID.Value;

        return await DbContext.UserGameCollections
                              .AsNoTracking()
                              .Where(userGame => userGame.UserProfileID == userProfileId)
                              .Include(userGame => userGame.Game)
                              .Select(userGame => userGame.Game)
                              .OrderBy(game => game.Name)
                              .ToListAsync();
    }

    protected async Task<List<PlatformModel>> LoadLibraryPlatformsAsync()
    {
        if (UserSession.CurrentUserID is null)
        {
            return [];
        }

        int userProfileId = UserSession.CurrentUserID.Value;

        return await DbContext.UserPlatformCollections
                              .AsNoTracking()
                              .Where(userPlatform => userPlatform.UserProfileID == userProfileId)
                              .Include(userPlatform => userPlatform.Platform)
                              .Select(userPlatform => userPlatform.Platform)
                              .OrderBy(platform => platform.Name)
                              .ToListAsync();
    }

    protected async Task<List<Company>> LoadCompaniesAsync()
    {
        return await DbContext.Companies
                              .OrderBy(company => company.Name)
                              .ToListAsync();
    }

    protected async Task<Game?> OpenAddGameToLibraryPopupAsync(IReadOnlyList<Company> companies, IReadOnlyList<PlatformModel> platforms)
    {
        Game? game = await PopupService.ShowAsync<AddGamePopup, Game>(
            new Dictionary<string, object?>
            {
                [nameof(AddGamePopup.Companies)] = companies,
                [nameof(AddGamePopup.Platforms)] = platforms,
                [nameof(AddGamePopup.OnCompanyAdded)] = new Func<Company, Task<Company?>>(AddCompanyFromSearch)
            });

        if (game is null)
        {
            return null;
        }

        await AddGameToUserLibraryAsync(game);
        return game;
    }

    protected async Task<PlatformModel?> OpenAddPlatformToLibraryPopupAsync(IReadOnlyList<Game> games, IReadOnlyList<Company> companies)
    {
        PlatformModel? platform = await PopupService.ShowAsync<AddPlatformPopup, PlatformModel>(
            new Dictionary<string, object?>
            {
                [nameof(AddPlatformPopup.Games)] = games,
                [nameof(AddPlatformPopup.Companies)] = companies,
                [nameof(AddPlatformPopup.OnCompanyAdded)] = new Func<Company, Task<Company?>>(AddCompanyFromSearch)
            });

        if (platform is null)
        {
            return null;
        }

        await AddPlatformToUserLibraryAsync(platform);
        return platform;
    }

    private async Task AddGameToUserLibraryAsync(Game game)
    {
        if (UserSession.CurrentUserID is null)
        {
            return;
        }

        game.GameCompanies = NormalizeGameCompanyIds(game.GameCompanies);
        List<GamePlatformRelation> ownerships = NormalizePlatformOwnerships(game.GamePlatforms);

        DbContext.Games.Add(game);
        await DbContext.SaveChangesAsync();

        int userProfileId = UserSession.CurrentUserID.Value;
        DbContext.UserGameCollections.Add(new UserGameCollection
                                          {
                                              UserProfileID = userProfileId,
                                              GameID = game.ID,
                                              Rating = Math.Clamp(game.Rating, 0, Game.MaxRating)
                                          });

        foreach (GamePlatformRelation ownership in ownerships)
        {
            DbContext.UserGamePlatformOwnerships.Add(new UserGamePlatformOwnership
                                                     {
                                                         UserProfileID = userProfileId,
                                                         GameID = game.ID,
                                                         PlatformID = ownership.PlatformID,
                                                         Ownership = ownership.Ownership
                                                     });
        }

        await DbContext.SaveChangesAsync();
    }

    private async Task AddPlatformToUserLibraryAsync(PlatformModel platform)
    {
        if (UserSession.CurrentUserID is null)
        {
            return;
        }

        platform.PlatformCompanies = NormalizePlatformCompanyIds(platform.PlatformCompanies);

        DbContext.Platforms.Add(platform);
        await DbContext.SaveChangesAsync();

        DbContext.UserPlatformCollections.Add(new UserPlatformCollection
                                              {
                                                  UserProfileID = UserSession.CurrentUserID.Value,
                                                  PlatformID = platform.ID
                                              });

        await DbContext.SaveChangesAsync();
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
            return existingCompany;
        }

        NormalizeCompanyDetails(newCompany);
        DbContext.Companies.Add(newCompany);
        await DbContext.SaveChangesAsync();
        return newCompany;
    }

    private static List<GameCompany> NormalizeGameCompanyIds(IEnumerable<GameCompany> companies)
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

    private static List<PlatformCompany> NormalizePlatformCompanyIds(IEnumerable<PlatformCompany> companies)
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

    private static List<GamePlatformRelation> NormalizePlatformOwnerships(IEnumerable<GamePlatformRelation> ownerships)
    {
        return ownerships
               .Where(ownership => ownership.PlatformID > 0
                                   && ownership.Ownership != OwnershipType.None)
               .GroupBy(ownership => new
                                     {
                                         ownership.PlatformID,
                                         ownership.Ownership
                                     })
               .Select(group => group.First())
               .OrderBy(ownership => ownership.PlatformID)
               .ThenBy(ownership => ownership.Ownership)
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

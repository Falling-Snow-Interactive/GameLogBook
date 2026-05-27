using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using VGL.Components.Elements.GameElements;
using VGL.Components.Popups;
using VGL.Models;
using VGL.Models.Games;
using VGL.Models.Games.Company;
using VGL.Models.Games.Platforms;
using VGL.Models.Users;
using VGL.Services;
using Company = VGL.Models.Companies.Company;
using GameView = VGL.Components.Elements.GameElements.GameView;
using PlatformModel = VGL.Models.Platforms.Platform;

namespace VGL.Components.Pages;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class GamesPage : CollectionPageBase<Game>
{
    private List<Company> companies = [];
    private List<PlatformModel> platforms = [];

    [Inject]
    private PopupService PopupService { get; set; } = null!;

    protected override DbSet<Game> EntitySet => DbContext.Games;

    protected override string GetSortKey(Game item)
    {
        return item.Name;
    }

    protected override IQueryable<Game> BuildQuery()
    {
        return EntitySet.Include(game => game.GameCompanies)
                        .ThenInclude(gameCompany => gameCompany.Company);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadCompaniesAsync();
        await LoadPlatformsAsync();
    }

    protected override async Task LoadItemsAsync()
    {
        if (UserSession.CurrentUserID is null)
        {
            Items = [];
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;
        List<UserGameCollection> userGames = await DbContext.UserGameCollections
                                                            .AsNoTracking()
                                                            .Where(userGame => userGame.UserProfileID == userProfileId)
                                                            .Include(userGame => userGame.Game)
                                                            .ThenInclude(game => game.GameCompanies)
                                                            .ThenInclude(gameCompany => gameCompany.Company)
                                                            .ToListAsync();

        List<UserGamePlatformOwnership> ownerships = await DbContext.UserGamePlatformOwnerships
                                                                    .AsNoTracking()
                                                                    .Where(ownership => ownership.UserProfileID == userProfileId)
                                                                    .ToListAsync();

        Items = userGames
                .Select(userGame =>
                        {
                            userGame.Game.Rating = userGame.Rating;
                            userGame.Game.GamePlatforms = ownerships
                                                          .Where(ownership => ownership.GameID == userGame.GameID)
                                                          .Select(ownership => new GamePlatformRelation
                                                                               {
                                                                                   GameID = ownership.GameID,
                                                                                   PlatformID = ownership.PlatformID,
                                                                                   Ownership = ownership.Ownership
                                                                               })
                                                          .ToList();

                            return userGame.Game;
                        })
                .OrderBy(GetSortKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    private async Task AddGame(Game game)
    {
        if (UserSession.CurrentUserID is null)
        {
            return;
        }

        game.GameCompanies = NormalizeCompanyIds(game.GameCompanies);
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
        AddOwnerships(userProfileId, game.ID, ownerships);

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

    private async Task UpdateGame(Game updatedGame)
    {
        Game? existingGame = await BuildQuery()
                                 .FirstOrDefaultAsync(game => game.ID == updatedGame.ID);

        if (existingGame is null)
        {
            return;
        }

        existingGame.IGDB = updatedGame.IGDB;
        existingGame.Name = updatedGame.Name.Trim();
        existingGame.GameType = updatedGame.GameType;
        existingGame.ReleaseDate = updatedGame.ReleaseDate;
        existingGame.Summary = string.IsNullOrWhiteSpace(updatedGame.Summary) ? null : updatedGame.Summary.Trim();
        existingGame.Cover = CopyImageRef(updatedGame.Cover);
        existingGame.Hero = CopyImageRef(updatedGame.Hero);
        existingGame.Logo = CopyImageRef(updatedGame.Logo);
        existingGame.Icon = CopyImageRef(updatedGame.Icon);
        existingGame.GameCompanies = NormalizeCompanyIds(updatedGame.GameCompanies);

        await UpdateUserGameDetails(updatedGame);
        await UpdateItemAsync();
    }

    private async Task RemoveGame(Game game)
    {
        if (UserSession.CurrentUserID is null)
        {
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;
        UserGameCollection? userGame = await DbContext.UserGameCollections
                                                      .FirstOrDefaultAsync(item => item.UserProfileID == userProfileId
                                                                                   && item.GameID == game.ID);

        if (userGame is not null)
        {
            DbContext.UserGameCollections.Remove(userGame);
        }

        List<UserGamePlatformOwnership> ownerships = await DbContext.UserGamePlatformOwnerships
                                                                    .Where(ownership => ownership.UserProfileID == userProfileId
                                                                                        && ownership.GameID == game.ID)
                                                                    .ToListAsync();
        DbContext.UserGamePlatformOwnerships.RemoveRange(ownerships);

        await DbContext.SaveChangesAsync();
        await LoadItemsAsync();
    }

    private List<Company> GetRelatedCompanies(Game game)
    {
        HashSet<int> companyIds = game.GetAllCompanyIDs().ToHashSet();

        return companies
               .Where(company => companyIds.Contains(company.ID))
               .OrderBy(company => company.Name)
               .ToList();
    }

    private async Task LoadCompaniesAsync()
    {
        companies = await DbContext.Companies
                                   .OrderBy(company => company.Name)
                                   .ToListAsync();
    }

    private async Task LoadPlatformsAsync()
    {
        platforms = await DbContext.Platforms
                                   .OrderBy(platform => platform.Name)
                                   .ToListAsync();
    }

    protected override async Task OpenAddPopup()
    {
        Game? game = await PopupService.ShowAsync<AddGamePopup, Game>(
            new Dictionary<string, object?>
            {
                [nameof(AddGamePopup.Companies)] = companies,
                [nameof(AddGamePopup.Platforms)] = platforms,
                [nameof(AddGamePopup.OnCompanyAdded)] = new Func<Company, Task<Company?>>(AddCompanyFromSearch)
            });

        if (game is not null)
        {
            await AddGame(game);
        }
    }

    private async Task OnClickGame(Game game)
    {
        Game selectedGame = new(game);
        bool? shouldEdit = await PopupService.ShowAsync<GameView, bool>(
            new Dictionary<string, object?>
            {
                [nameof(GameView.Game)] = selectedGame
            });

        if (shouldEdit == true)
        {
            await OpenEditPopup(selectedGame);
        }
    }

    private static List<GameCompany> NormalizeCompanyIds(IEnumerable<GameCompany> companies)
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

    private async Task UpdateUserGameDetails(Game updatedGame)
    {
        if (UserSession.CurrentUserID is null)
        {
            return;
        }

        int userProfileId = UserSession.CurrentUserID.Value;
        UserGameCollection? userGame = await DbContext.UserGameCollections
                                                      .FirstOrDefaultAsync(item => item.UserProfileID == userProfileId
                                                                                   && item.GameID == updatedGame.ID);

        if (userGame is null)
        {
            DbContext.UserGameCollections.Add(new UserGameCollection
                                              {
                                                  UserProfileID = userProfileId,
                                                  GameID = updatedGame.ID,
                                                  Rating = Math.Clamp(updatedGame.Rating, 0, Game.MaxRating)
                                              });
        }
        else
        {
            userGame.Rating = Math.Clamp(updatedGame.Rating, 0, Game.MaxRating);
        }

        List<UserGamePlatformOwnership> existingOwnerships = await DbContext.UserGamePlatformOwnerships
                                                                           .Where(ownership => ownership.UserProfileID == userProfileId
                                                                                               && ownership.GameID == updatedGame.ID)
                                                                           .ToListAsync();
        DbContext.UserGamePlatformOwnerships.RemoveRange(existingOwnerships);

        AddOwnerships(userProfileId, updatedGame.ID, NormalizePlatformOwnerships(updatedGame.GamePlatforms));
    }

    private void AddOwnerships(int userProfileId, int gameId, IEnumerable<GamePlatformRelation> ownerships)
    {
        foreach (GamePlatformRelation ownership in ownerships)
        {
            DbContext.UserGamePlatformOwnerships.Add(new UserGamePlatformOwnership
                                                     {
                                                         UserProfileID = userProfileId,
                                                         GameID = gameId,
                                                         PlatformID = ownership.PlatformID,
                                                         Ownership = ownership.Ownership
                                                     });
        }
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

    private async Task OpenEditPopup(Game game)
    {
        Game? updatedGame = await PopupService.ShowAsync<AddGamePopup, Game>(
            new Dictionary<string, object?>
            {
                [nameof(AddGamePopup.InitialGame)] = game,
                [nameof(AddGamePopup.Companies)] = companies,
                [nameof(AddGamePopup.Platforms)] = platforms,
                [nameof(AddGamePopup.OnCompanyAdded)] = new Func<Company, Task<Company?>>(AddCompanyFromSearch)
            });

        if (updatedGame is not null)
        {
            await UpdateGame(updatedGame);
        }
    }
}

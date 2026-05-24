using GameLogBook.Data;
using GameLogBook.Models.Games.Company;
using GameLogBook.Models.Games.Platform;
using GameLogBook.Utilities;
using Microsoft.AspNetCore.Components;

namespace GameLogBook.Models.Games;

public class Game
{
    // Constants
    public const int MaxRating = 5;
    
    // Game Properties
    public int ID { get; set; }
    public long? IgdbId { get; set; }
    
    public GameType GameType { get; set; }

    public string Name { get; set; }
    public string? Summary { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public Cover? Cover { get; set; }

    public List<GameCompany> GameCompanies { get; set; }
    public List<GamePlatform> GamePlatforms { get; set; }
    
    public int Rating { get; set; }
    
    // Inject
    [Inject]
    protected GameLogBookDbContext DbContext { get; set; } = null!;

    public Game()
    {
        ID = -1;
        IgdbId = -1;

        GameType = GameType.None;

        Name = string.Empty;
        Summary = string.Empty;
        ReleaseDate = DateOnly.MinValue;
        Cover = null;

        GameCompanies = [];
        GamePlatforms = [];
    }

    public Game(IGDB.Models.Game igdb)
    {
        ID = -1;
        IgdbId = igdb.Id;

        GameType = igdb.GameType.Value.Type.ToLower() switch
                   {
                       "expansion" => GameType.Expansion,
                       "port" => GameType.Port,
                       "dlc" => GameType.DLC,
                       "bundle" => GameType.Bundle,
                       "pack/addon" => GameType.PackAddon,
                       "mod" => GameType.Mod,
                       "remake" => GameType.Remake,
                       "remaster" => GameType.Remaster,
                       _ => GameType.Base,
                   };
        
        Name = igdb.Name;
        Summary = igdb.Summary;
        if(igdb.FirstReleaseDate.HasValue)
        {
            ReleaseDate = DateOnly.FromDateTime(igdb.FirstReleaseDate.Value.Date);
        }
        else
        {
            ReleaseDate = DateOnly.FromDateTime(DateTime.Now);
        }

        if (igdb.Cover?.Value.Url is not null)
        {
            Cover = new Cover()
                    {
                        PendingImageUrl = IGDBUtility.CoverUrlToBigCoverUrl(igdb.Cover.Value.Url),
                    };
        }
    }

    public Game(Game copyFrom)
    {
        ID = copyFrom.ID;
        IgdbId = copyFrom.IgdbId;
        
        GameType = copyFrom.GameType;
        
        Name = copyFrom.Name;
        Summary = copyFrom.Summary;
        ReleaseDate = copyFrom.ReleaseDate;
        Cover = copyFrom.Cover;
        
        GameCompanies = copyFrom.GameCompanies;
        GamePlatforms = copyFrom.GamePlatforms;

        Rating = copyFrom.Rating;
    }
    
    #region Company

    private List<int> GetCompanyIDs(GameCompanyRole role)
    {
        List<int> results = GameCompanies
                            .Where(gameCompany => gameCompany.Role == role)
                            .Select(gameCompany => gameCompany.CompanyID)
                            .Where(companyId => companyId > 0)
                            .Distinct()
                            .Order()
                            .ToList();
        return results;
    }

    public List<int> GetAllCompanyIDs()
    {
        List<int> devs = GetCompanyIDs(GameCompanyRole.Developer);
        List<int> pubs = GetCompanyIDs(GameCompanyRole.Publisher);
        devs.AddRange(pubs);
        return devs;
    }

    public void AddCompaniesByID(GameCompanyRole role, IEnumerable<int> companyIDs)
    {
        int[] normalizedCompanyIDs = companyIDs
                                     .Where(companyID => companyID > 0)
                                     .Distinct()
                                     .Order()
                                     .ToArray();

        GameCompanies.RemoveAll(gameCompany => gameCompany.Role == role
                                               && !normalizedCompanyIDs.Contains(gameCompany.CompanyID));

        HashSet<int> existingCompanyIds = GameCompanies
                                          .Where(gameCompany => gameCompany.Role == role)
                                          .Select(gameCompany => gameCompany.CompanyID)
                                          .ToHashSet();

        foreach (int companyId in normalizedCompanyIDs.Where(companyId => !existingCompanyIds.Contains(companyId)))
        {
            GameCompanies.Add(new GameCompany
                              {
                                  Game = this,
                                  CompanyID = companyId,
                                  Role = role
                              });
        }
    }

    public List<Companies.Company> GetDevelopers()
    {
        return GameCompanies
               .Where(gameCompany => gameCompany.Role == GameCompanyRole.Developer)
               .Select(gameCompany => gameCompany.Company)
               .DistinctBy(company => company.ID)
               .OrderBy(company => company.Name)
               .ToList();
    }
    
    public List<int> GetDeveloperIDs()
    {
        return GetCompanyIDs(GameCompanyRole.Developer);
    }

    public List<int> GetPublisherIDs()
    {
        return GetCompanyIDs(GameCompanyRole.Publisher);
    }
    
    #endregion
    
    #region Platforms

    private int[] GetPlatformIDs()
    {
        return GamePlatforms
               .Select(gamePlatform => gamePlatform.PlatformID)
               .Where(platformID => platformID > 0)
               .Distinct()
               .Order()
               .ToArray();
    }
    
    private void SetPlatformIDs(OwnershipType ownershipType, IEnumerable<int> platformIDs)
    {
        int[] normalizedPlatformIDs = platformIDs
                                      .Where(platformID => platformID > 0)
                                      .Distinct()
                                      .Order()
                                      .ToArray();

        GamePlatforms.RemoveAll(gamePlatform => gamePlatform.Ownership == ownershipType
                                                && !normalizedPlatformIDs.Contains(gamePlatform.PlatformID));

        HashSet<int> existingCompanyIds = GamePlatforms
                                          .Where(gamePlatform => gamePlatform.Ownership == ownershipType)
                                          .Select(gamePlatform => gamePlatform.PlatformID)
                                          .ToHashSet();

        foreach (int companyID in normalizedPlatformIDs.Where(companyId => !existingCompanyIds.Contains(companyId)))
        {
            GamePlatforms.Add(new GamePlatform
                              {
                                  Game = this,
                                  PlatformID = companyID,
                                  Ownership = ownershipType,
                              });
        }
    }
    
    #endregion
}

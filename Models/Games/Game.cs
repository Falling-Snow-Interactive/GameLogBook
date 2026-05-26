using VGL.Models.Games.Company;
using VGL.Models.Games.Platforms;
using VGL.Models.Libraries.Entries;

namespace VGL.Models.Games;

public class Game : ILibraryEntry
{
    #region Constants
    public const int MaxRating = 10;
    #endregion
    
    // Database
    public int ID { get; set; }
    
    // Game Information
    public string Name { get; set; }
    public string? Summary { get; set; }
    
    public GameType GameType { get; set; }
    
    public DateOnly? ReleaseDate { get; set; }
    
    // Images
    public ImageRef? Cover { get; set; }
    public ImageRef? Hero { get; set; }
    public ImageRef? Logo { get; set; }
    public ImageRef? Icon { get; set; }
    
    // User Set Information
    public int Rating { get; set; }

    // Relations
    public List<GameCompany> GameCompanies { get; set; }
    public List<GamePlatformRelation> GamePlatforms { get; set; }
    
    // Online APIs
    public long? IGDB { get; set; }
    
    // ILibaryEntry
    public DateOnly? Date => ReleaseDate;
    
    #region Constructors

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">Name of the game. *Required*</param>
    public Game(string name)
    {
        // Database
        ID = -1;

        // Information
        Name = name;
        GameType = GameType.None;
        ReleaseDate = DateOnly.MinValue;
        Summary = string.Empty;
        
        // Images
        Cover = null;
        Hero = null;
        Logo = null;
        Icon = null;

        // Relations
        GameCompanies = [];
        GamePlatforms = [];
        
        // APIs
        IGDB = -1;
    }

    public Game(Game copyFrom)
    {
        ID = copyFrom.ID;
        IGDB = copyFrom.IGDB;
        
        GameType = copyFrom.GameType;
        
        Name = copyFrom.Name;
        Summary = copyFrom.Summary;
        ReleaseDate = copyFrom.ReleaseDate;
        
        Cover = copyFrom.Cover;
        Hero = copyFrom.Hero;
        Logo = copyFrom.Logo;
        Icon = copyFrom.Icon;
        
        GameCompanies = copyFrom.GameCompanies;
        GamePlatforms = copyFrom.GamePlatforms;

        Rating = copyFrom.Rating;
    }
    
    #endregion
    
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
            GamePlatforms.Add(new GamePlatformRelation
                              {
                                  Game = this,
                                  PlatformID = companyID,
                                  Ownership = ownershipType,
                              });
        }
    }
    
    #endregion
}

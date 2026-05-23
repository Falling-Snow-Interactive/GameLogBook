using GameLogBook.Models.Games.Company;
using GameLogBook.Models.Games.Platform;

namespace GameLogBook.Models.Games;

public class Game
{
    public int Id { get; set; }

    public long IgdbId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Summary { get; set; }
    
    public GameType GameType { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public Cover? Cover { get; set; }

    public List<GameCompany> GameCompanies { get; set; } = [];

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int[] DeveloperCompanyIds
    {
        get => GetCompanyIds(GameCompanyRole.Developer);
        set => SetCompanyIds(GameCompanyRole.Developer, value);
    }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int[] PublisherCompanyIds
    {
        get => GetCompanyIds(GameCompanyRole.Publisher);
        set => SetCompanyIds(GameCompanyRole.Publisher, value);
    }
    
    public int Rating { get; set; }

    public List<GamePlatform> GamePlatforms { get; set; } = [];

    public int[] PlatformIDs
    {
        get => GetPlatformIDs();
    }

    private int[] GetCompanyIds(GameCompanyRole role)
    {
        return GameCompanies
               .Where(gameCompany => gameCompany.Role == role)
               .Select(gameCompany => gameCompany.CompanyID)
               .Where(companyId => companyId > 0)
               .Distinct()
               .Order()
               .ToArray();
    }

    private void SetCompanyIds(GameCompanyRole role, IEnumerable<int> companyIds)
    {
        int[] normalizedCompanyIds = companyIds
                                     .Where(companyId => companyId > 0)
                                     .Distinct()
                                     .Order()
                                     .ToArray();

        GameCompanies.RemoveAll(gameCompany => gameCompany.Role == role
                                               && !normalizedCompanyIds.Contains(gameCompany.CompanyID));

        HashSet<int> existingCompanyIds = GameCompanies
                                          .Where(gameCompany => gameCompany.Role == role)
                                          .Select(gameCompany => gameCompany.CompanyID)
                                          .ToHashSet();

        foreach (int companyId in normalizedCompanyIds.Where(companyId => !existingCompanyIds.Contains(companyId)))
        {
            GameCompanies.Add(new GameCompany
                              {
                                  Game = this,
                                  CompanyID = companyId,
                                  Role = role
                              });
        }
    }

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
}

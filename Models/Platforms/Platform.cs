using VGL.Models.Libraries.Entries;
using VGL.Models.Platforms.Company;

namespace VGL.Models.Platforms;

public class Platform(string name) : ILibraryEntry
{
    // Database
    public int ID { get; init; }

    // Information
    public string Name { get; set; } = name;
    public string? ShortName { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Summary { get; set; }
    
    // Images
    public ImageRef? Cover { get; set; }
    public ImageRef? Hero { get; set; }
    public ImageRef? Logo { get; set; }
    public ImageRef? Icon { get; set; }
    
    // Relations
    public List<PlatformCompany> PlatformCompanies { get; set; } = [];
    
    // APIs
    public long? IGDB { get; set; }
    
    // Interface Redirects
    public DateOnly? Date => ReleaseDate;

    #region Constructors
    
    public Platform(Platform other) : this(other.Name)
    {
        ID = other.ID;

        Name = other.Name;
        ShortName = other.ShortName;
        ReleaseDate = other.ReleaseDate;
        Summary = other.Summary;
        
        PlatformCompanies = other.PlatformCompanies
                                 .Select(platformCompany => new PlatformCompany
                                                            {
                                                                PlatformID = platformCompany.PlatformID,
                                                                CompanyID = platformCompany.CompanyID,
                                                                Role = platformCompany.Role,
                                                                Company = platformCompany.Company
                                                            })
                                 .ToList();
        
        Cover = other.Cover;
        Hero = other.Hero;
        Logo = other.Logo;
        Icon = other.Icon;
        
        IGDB = other.IGDB;
    }
    
    #endregion

    #region Companies

    private List<int> GetCompanyIDs(PlatformCompanyRole role)
    {
        return PlatformCompanies
               .Where(platformCompany => platformCompany.Role == role)
               .Select(platformCompany => platformCompany.CompanyID)
               .Where(companyId => companyId > 0)
               .Distinct()
               .Order()
               .ToList();
    }

    public List<int> GetDeveloperIDs()
    {
        return GetCompanyIDs(PlatformCompanyRole.Developer);
    }

    public List<int> GetAllCompanyIDs()
    {
        return GetDeveloperIDs();
    }

    public void AddCompaniesByID(PlatformCompanyRole role, IEnumerable<int> companyIDs)
    {
        int[] normalizedCompanyIDs = companyIDs
                                     .Where(companyID => companyID > 0)
                                     .Distinct()
                                     .Order()
                                     .ToArray();

        PlatformCompanies.RemoveAll(platformCompany => platformCompany.Role == role
                                                       && !normalizedCompanyIDs.Contains(platformCompany.CompanyID));

        HashSet<int> existingCompanyIds = PlatformCompanies
                                          .Where(platformCompany => platformCompany.Role == role)
                                          .Select(platformCompany => platformCompany.CompanyID)
                                          .ToHashSet();

        foreach (int companyId in normalizedCompanyIDs.Where(companyId => !existingCompanyIds.Contains(companyId)))
        {
            PlatformCompanies.Add(new PlatformCompany
                                  {
                                      Platform = this,
                                      CompanyID = companyId,
                                      Role = role
                                  });
        }
    }

    #endregion
}

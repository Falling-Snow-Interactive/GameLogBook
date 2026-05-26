using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Models.Platforms.Company;

public class PlatformCompany
{
    public int PlatformID { get; set; }

    public Platform Platform { get; set; } = null!;

    public int CompanyID { get; set; }

    public Companies.Company Company { get; set; } = null!;

    public PlatformCompanyRole Role { get; set; }
}

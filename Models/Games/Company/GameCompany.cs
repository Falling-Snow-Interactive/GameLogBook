namespace GameLogBook.Models.Games.Company;

public class GameCompany
{
    public int GameID { get; set; }

    public Game Game { get; set; } = null!;

    public int CompanyID { get; set; }

    public Companies.Company Company { get; set; } = null!;

    public GameCompanyRole Role { get; set; }
}

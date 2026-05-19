using GameLogBook.Models.Companies;

namespace GameLogBook.Models.Games;

public class GameCompany
{
    public int GameId { get; set; }

    public Game Game { get; set; } = null!;

    public int CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public GameCompanyRole Role { get; set; }
}

public enum GameCompanyRole
{
    Developer = 1,
    Publisher = 2
}

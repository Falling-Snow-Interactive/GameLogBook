namespace GameLogBook.Models.Library;

public class Game
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public Cover? Cover { get; init; }
    
    public string? Developer { get; init; } = string.Empty;

    public string? Publisher { get; init; } = string.Empty;

    public DateOnly? ReleaseDate { get; init; }

    public string? ReleaseDateString
    {
        get
        {
            if (ReleaseDate is null)
            {
                return null;
            }

            int day = ReleaseDate.Value.Day;

            string suffix = day switch
                            {
                                11 or 12 or 13 => "th",
                                _ => (day % 10) switch
                                     {
                                         1 => "st",
                                         2 => "nd",
                                         3 => "rd",
                                         _ => "th",
                                     }
                            };

            return $"{ReleaseDate.Value:MMMM} {day}{suffix}, {ReleaseDate.Value:yyyy}";
        }
    }
    
    public string? Summary { get; init; }

    public string Title => Name;

    public string? CoverUrl => Cover?.Url;

    public string? CoverBig =>
        Cover?.Url?.Replace("t_thumb", "t_cover_big");
    
}
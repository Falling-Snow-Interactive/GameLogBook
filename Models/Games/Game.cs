using System.Text.Json.Serialization;

namespace GameLogBook.Models.Games;

public class Game
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Title => Name;
    
    public Cover? Cover { get; set; }

    #region Release Date
    
    [JsonPropertyName("first_release_date")]
    public long? FirstReleaseDateUnix { get; set; }

    public DateOnly? ReleaseDate
    {
        get => FirstReleaseDateUnix.HasValue 
                   ? DateOnly.FromDateTime(DateTimeOffset
                                           .FromUnixTimeSeconds(FirstReleaseDateUnix
                                                                    .Value)
                                           .UtcDateTime)
                   : null;
    }
    
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
                                         _ => "th"
                                     }
                            };

            return $"{ReleaseDate.Value:MMMM} {day}{suffix}, {ReleaseDate.Value:yyyy}";
        }
    }
    
    #endregion
    
    [JsonPropertyName("involved_companies")]
    public int[]? Companies { get; set; }
    public string? Summary { get; set; }
}

public class Cover
{
    private const string ThumbId = "t_thumb";
    private const string CoverBigId = "t_cover_big";
    
    public string? Url { get; set; }
    public string? CoverBig => Url?.Replace(ThumbId, CoverBigId);
}
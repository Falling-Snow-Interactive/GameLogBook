namespace GameLogBook.Models.Games;

public class Cover
{
    public string Url { get; set; } = string.Empty;
    public string BigCoverUrl => Url.Replace("t_thumb", "t_cover_big");
}
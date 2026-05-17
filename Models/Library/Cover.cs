using System.Text.Json.Serialization;

namespace GameLogBook.Models;

public class Cover
{
    private const string ThumbId = "t_thumb";
    private const string CoverBigId = "t_cover_big";

    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public int Width { get; set; }

    public int GameId { get; set; }

    public string CoverBig =>
        Url.Replace(ThumbId, CoverBigId);
}
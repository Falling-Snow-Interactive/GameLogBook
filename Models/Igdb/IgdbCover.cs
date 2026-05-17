using System.Text.Json.Serialization;
using GameLogBook.Models.Library;

namespace GameLogBook.Models.Igdb;

public class IgdbCover
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("game")]
    public int GameId { get; set; }

    public Cover ToLibraryCover()
    {
        return new Cover
               {
                   Url = Url,
                   Width = Width,
                   GameId = GameId
               };
    }
}
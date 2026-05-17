using System.Text.Json.Serialization;

namespace GameLogBook.Models.Igdb;

public class IgdbInvolvedCompany
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("developer")]
    public bool Developer { get; set; }

    [JsonPropertyName("publisher")]
    public bool Publisher { get; set; }

    [JsonPropertyName("company")]
    public IgdbCompany? Company { get; set; }
}
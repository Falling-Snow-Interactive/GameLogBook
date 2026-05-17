using System.Text.Json.Serialization;

namespace GameLogBook.Models.Igdb;

public class IgdbCompany
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
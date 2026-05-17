using System.Text.Json.Serialization;

namespace GameLogBook.Models;

public class CompanyInfo
{
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
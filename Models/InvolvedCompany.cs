using System.Text.Json.Serialization;

namespace GameLogBook.Models;

public class InvolvedCompany
{
    public int Id { get; set; }

    [JsonPropertyName("developer")]
    public bool Developer { get; set; }

    [JsonPropertyName("publisher")]
    public bool Publisher { get; set; }

    [JsonPropertyName("company")]
    public CompanyInfo? Company { get; set; }
}
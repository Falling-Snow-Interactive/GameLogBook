using System.Text.Json.Serialization;
using GameLogBook.Models.Library;

namespace GameLogBook.Models.Igdb;

public class Igdb
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("checksum")]
    public Guid Checksum { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("first_release_date")]
    public long? FirstReleaseDate { get; set; }

    [JsonPropertyName("cover")]
    public IgdbCover? Cover { get; set; }

    [JsonPropertyName("involved_companies")]
    public IgdbInvolvedCompany[] InvolvedCompanies { get; set; } = [];

    public Game ToLibraryGame()
    {
        return new Game
               {
                   Id = Id,
                   Name = Name,
                   Summary = Summary,
                   ReleaseDate = ToDateOnly(FirstReleaseDate),
                   Cover = Cover?.ToLibraryCover(),
                   Developer = GetCompanyNames(company => company.Developer),
                   Publisher = GetCompanyNames(company => company.Publisher)
               };
    }

    private static DateOnly? ToDateOnly(long? unix)
    {
        return unix.HasValue
                   ? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unix.Value).UtcDateTime)
                   : null;
    }

    private string GetCompanyNames(Func<IgdbInvolvedCompany, bool> predicate)
    {
        return string.Join(", ", InvolvedCompanies
                                 .Where(predicate)
                                 .Select(company => company.Company?.Name)
                                 .Where(name => !string.IsNullOrWhiteSpace(name))
                                 .Distinct());
    }
}
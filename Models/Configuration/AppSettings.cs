namespace VGL.Models.Configuration;

public class AppSettings
{
    public const int SingletonId = 1;

    public int ID { get; set; } = SingletonId;

    public string SteamGridDbApiKey { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

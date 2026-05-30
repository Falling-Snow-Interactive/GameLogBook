namespace VGL.Services.NowPlaying;

public sealed class StartSessionRequest
{
    public int? ExistingPlaythroughID { get; set; }

    public int? GameID { get; set; }

    public int? PlatformID { get; set; }

    public string? NewPlaythroughName { get; set; }
}

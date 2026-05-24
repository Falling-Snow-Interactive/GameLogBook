namespace GameLogBook.Utilities;

public static class IGDBUtility
{
    public static string CoverUrlToBigCoverUrl(string url)
    {
        return url.Replace("/t_thumb/", "/t_cover_big/", StringComparison.OrdinalIgnoreCase);
    }
}
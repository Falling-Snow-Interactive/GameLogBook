namespace VGL.Data;

internal static class DatabasePathResolver
{
    private const string DatabaseFileName = "GameLogBook.db";
    private const string ApplicationId = "com.kiradinan.gamelogbook";

    public static string GetRuntimeDatabasePath()
    {
        string path = GetConfiguredPath()
                      ?? GetMacAppContainerDatabasePath()
                      ?? Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
        EnsureParentDirectory(path);
        return path;
    }

    public static string GetDesignTimeDatabasePath()
    {
        string path = GetConfiguredPath()
                      ?? GetMacAppContainerDatabasePath()
                      ?? Path.Combine(Directory.GetCurrentDirectory(), DatabaseFileName);
        EnsureParentDirectory(path);
        return path;
    }

    private static string? GetConfiguredPath()
    {
        string? configuredPath = Environment.GetEnvironmentVariable("GAMELOGBOOK_DB_PATH");
        return string.IsNullOrWhiteSpace(configuredPath)
                   ? null
                   : Path.GetFullPath(configuredPath);
    }

    private static string? GetMacAppContainerDatabasePath()
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsMacCatalyst())
        {
            return null;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library",
            "Containers",
            ApplicationId,
            "Data",
            "Library",
            "Application Support",
            DatabaseFileName);
    }

    private static void EnsureParentDirectory(string databasePath)
    {
        string? directory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}

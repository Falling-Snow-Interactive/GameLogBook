using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VGL.Data;

public class GameLogBookDbContextFactory : IDesignTimeDbContextFactory<GameLogBookDbContext>
{
    public GameLogBookDbContext CreateDbContext(string[] args)
    {
        string databasePath = ResolveDatabasePath();

        DbContextOptions<GameLogBookDbContext> options = new DbContextOptionsBuilder<GameLogBookDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new GameLogBookDbContext(options);
    }

    private static string ResolveDatabasePath()
    {
        string? configuredPath = Environment.GetEnvironmentVariable("GAMELOGBOOK_DB_PATH");

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            string fullPath = Path.GetFullPath(configuredPath);
            EnsureParentDirectory(fullPath);
            return fullPath;
        }

        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string defaultPath = Path.Combine(
            homeDirectory,
            "Library",
            "Containers",
            "com.kiradinan.gamelogbook",
            "Data",
            "Library",
            "Application Support",
            "GameLogBook.db");

        EnsureParentDirectory(defaultPath);
        return defaultPath;
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

using VGL.Data;
using Microsoft.EntityFrameworkCore;

string databasePath = ResolveDatabasePath();

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

string command = args[0].ToLowerInvariant();

DbContextOptions<GameLogBookDbContext> options = new DbContextOptionsBuilder<GameLogBookDbContext>()
    .UseSqlite($"Data Source={databasePath}")
    .Options;

await using GameLogBookDbContext dbContext = new(options);

switch (command)
{
    case "rebuild":
        Console.WriteLine($"Using database: {databasePath}");
        Console.WriteLine("Deleting database if it exists...");
        await dbContext.Database.EnsureDeletedAsync();
        Console.WriteLine("Applying migrations...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Database rebuild complete.");
        return 0;

    case "migrate":
        Console.WriteLine($"Using database: {databasePath}");
        Console.WriteLine("Applying migrations...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Database migration complete.");
        return 0;

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintUsage();
        return 1;
}

static string ResolveDatabasePath()
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

static void EnsureParentDirectory(string databasePath)
{
    string? directory = Path.GetDirectoryName(databasePath);

    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

static void PrintUsage()
{
    Console.WriteLine("Usage: dotnet run --project tools/GameLogBook.DbTool -- [rebuild|migrate]");
}

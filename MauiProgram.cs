using System.Reflection;
using GameLogBook.Data;
using GameLogBook.Models.Configuration;
using GameLogBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameLogBook;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        AddEmbeddedJsonConfiguration(builder.Configuration, "GameLogBook.appsettings.json");

#if DEBUG
        AddEmbeddedJsonConfiguration(builder.Configuration, "GameLogBook.appsettings.Development.json");
#endif

        builder.Services.Configure<IgdbSettings>(builder.Configuration.GetSection("Igdb"));
        builder.Services.AddSingleton<IgdbClientProvider>();

        string databasePath = DatabasePathResolver.GetRuntimeDatabasePath();

        builder.Services.AddDbContext<GameLogBookDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        MauiApp app = builder.Build();

        using IServiceScope scope = app.Services.CreateScope();
        GameLogBookDbContext dbContext = scope.ServiceProvider.GetRequiredService<GameLogBookDbContext>();
        dbContext.Database.Migrate();
        EnsurePlatformCoverUrlColumn(dbContext);

        return app;
    }

    private static void EnsurePlatformCoverUrlColumn(GameLogBookDbContext dbContext)
    {
        const string migrationId = "20260519133000_AddPlatformCoverUrl";
        const string productVersion = "10.0.8";

        if (HasColumn(dbContext, "Platforms", "CoverUrl"))
        {
            EnsureMigrationHistoryEntry(dbContext, migrationId, productVersion);
            return;
        }

        dbContext.Database.ExecuteSqlRaw("""
                                         ALTER TABLE "Platforms"
                                         ADD COLUMN "CoverUrl" TEXT NULL;
                                         """);

        EnsureMigrationHistoryEntry(dbContext, migrationId, productVersion);
    }

    private static bool HasColumn(GameLogBookDbContext dbContext, string tableName, string columnName)
    {
        using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"""
                               SELECT COUNT(*)
                               FROM pragma_table_info('{tableName}')
                               WHERE name = '{columnName}';
                               """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            command.Connection?.Open();
        }

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void EnsureMigrationHistoryEntry(GameLogBookDbContext dbContext, string migrationId, string productVersion)
    {
        using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"""
                               SELECT COUNT(*)
                               FROM "__EFMigrationsHistory"
                               WHERE "MigrationId" = '{migrationId}';
                               """;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
        {
            command.Connection?.Open();
        }

        if (Convert.ToInt32(command.ExecuteScalar()) > 0)
        {
            return;
        }

        dbContext.Database.ExecuteSqlRaw(
            """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ({0}, {1});
            """,
            migrationId,
            productVersion);
    }

    private static void AddEmbeddedJsonConfiguration(IConfigurationBuilder configuration, string resourceName)
    {
        Stream? stream = Assembly.GetExecutingAssembly()
                                 .GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return;
        }

        configuration.AddJsonStream(stream);
    }
}

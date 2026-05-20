using System.Reflection;
using GameLogBook.Data;
using GameLogBook.Models.Configuration;
using GameLogBook.Services;
using Microsoft.EntityFrameworkCore;
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

        return app;
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

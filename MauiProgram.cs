using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VGL.Data;
using VGL.Models.Configuration;
using VGL.Services;
using VGL.Services.UserProfiles;

namespace VGL;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
        
        #if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
        #endif

        AddEmbeddedJsonConfiguration(builder.Configuration, "appsettings.json");
        
        #if DEBUG
        AddEmbeddedJsonConfiguration(builder.Configuration, "appsettings.Development.json");
        #endif

        builder.Services.Configure<IgdbSettings>(builder.Configuration.GetSection("Igdb"));
        builder.Services.Configure<SteamGridDbSettings>(builder.Configuration.GetSection("SteamGridDb"));
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<IGDBClientProvider>();
        builder.Services.AddSingleton<SteamGridDbClientProvider>();
        builder.Services.AddSingleton<SteamGridDbArtworkService>();
        builder.Services.AddSingleton<LocalImageService>();
        builder.Services.AddScoped<PopupService>();
        builder.Services.AddScoped<UserProfileSession>();

        string databasePath = DatabasePathResolver.GetRuntimeDatabasePath();

        builder.Services.AddDbContext<GameLogBookDbContext>(options => options.UseSqlite($"Data Source={databasePath}")
                                                                              .ConfigureWarnings(warnings =>
                                                                                                     warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

        MauiApp app = builder.Build();

        using IServiceScope scope = app.Services.CreateScope();
        GameLogBookDbContext dbContext = scope.ServiceProvider.GetRequiredService<GameLogBookDbContext>();
        dbContext.Database.Migrate();

        return app;
    }

    private static void AddEmbeddedJsonConfiguration(IConfigurationBuilder configuration, string resourceFileName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string? resourceName = assembly.GetManifestResourceNames()
                                       .FirstOrDefault(name =>
                                                           name.EndsWith($".{resourceFileName}",
                                                                         StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return;
        }

        Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        configuration.AddJsonStream(stream);
    }
}

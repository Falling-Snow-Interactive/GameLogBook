using GameLogBook.Components;
using GameLogBook.Data;
using GameLogBook.Models.Configuration;
using Microsoft.EntityFrameworkCore;
using IGDB;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services.Configure<IgdbSettings>(builder.Configuration
                                                .GetSection("Igdb"));

string igdbClientId = builder.Configuration["Igdb:ClientId"]!;
string igdbClientSecret = builder.Configuration["Igdb:ClientSecret"]!;

builder.Services.AddSingleton(_ => IGDBClient.CreateWithDefaults(igdbClientId, igdbClientSecret));

builder.Services.AddDbContext<GameLogBookDbContext>(options =>
                                                        options.UseSqlite("Data Source=GameLogBook.db"));

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    GameLogBookDbContext dbContext = scope.ServiceProvider.GetRequiredService<GameLogBookDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
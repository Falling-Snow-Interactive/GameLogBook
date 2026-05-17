using GameLogBook.Components;
using GameLogBook.Models.Configuration;
using GameLogBook.Services;
using GameLogBook.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services.Configure<IgdbSettings>(builder.Configuration.GetSection("Igdb"));
builder.Services.AddHttpClient<IgdbService>();

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
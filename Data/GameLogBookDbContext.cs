using GameLogBook.Models;
using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using GameLogBook.Models.Games.Company;
using GameLogBook.Models.Games.Platform;
using Microsoft.EntityFrameworkCore;
using PlatformModel = GameLogBook.Models.Platforms.Platform;

namespace GameLogBook.Data;

public class GameLogBookDbContext(DbContextOptions<GameLogBookDbContext> options) 
    : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<PlatformModel> Platforms => Set<PlatformModel>();
    
    public DbSet<Playthrough> Playthroughs => Set<Playthrough>();
    
    // Relational DBs
    public DbSet<GameCompany> GameCompanies => Set<GameCompany>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Game>().OwnsOne(game => game.Cover);
        modelBuilder.Entity<Game>().OwnsOne(game => game.Hero);
        modelBuilder.Entity<Game>().OwnsOne(game => game.Logo);
        modelBuilder.Entity<Game>().OwnsOne(game => game.Icon);
        
        modelBuilder.Entity<PlatformModel>().OwnsOne(platform => platform.Cover);
        modelBuilder.Entity<PlatformModel>().OwnsOne(platform => platform.Hero);
        modelBuilder.Entity<PlatformModel>().OwnsOne(platform => platform.Logo);
        modelBuilder.Entity<PlatformModel>().OwnsOne(platform => platform.Icon);

        SetupRelationalDbs(modelBuilder);
    }

    private void SetupRelationalDbs(ModelBuilder modelBuilder)
    {
        SetupGameCompanyRelationalDb(modelBuilder);
        SetupGamePlatformRelationalDb(modelBuilder);
    }

    private void SetupGameCompanyRelationalDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameCompany>()
                    .HasKey(gameCompany => new
                                           {
                                               GameId = gameCompany.GameID,
                                               CompanyId = gameCompany.CompanyID,
                                               gameCompany.Role,
                                           });

        modelBuilder.Entity<GameCompany>()
                    .HasOne(gameCompany => gameCompany.Game)
                    .WithMany(game => game.GameCompanies)
                    .HasForeignKey(gameCompany => gameCompany.GameID)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameCompany>()
                    .HasOne(gameCompany => gameCompany.Company)
                    .WithMany()
                    .HasForeignKey(gameCompany => gameCompany.CompanyID)
                    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Company>()
                    .HasIndex(company => company.ID)
                    .IsUnique()
                    .HasFilter($"{nameof(Company.ID)} IS NOT NULL");
    }
    
    private void SetupGamePlatformRelationalDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GamePlatform>()
                    .HasKey(gamePlatform => new
                                            {
                                                GameId = gamePlatform.GameID,
                                                Platform = gamePlatform.PlatformID,
                                                gamePlatform.Ownership,
                                            });

        modelBuilder.Entity<GamePlatform>()
                    .HasOne(gamePlatform => gamePlatform.Game)
                    .WithMany(game => game.GamePlatforms)
                    .HasForeignKey(gamePlatform => gamePlatform.GameID)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GamePlatform>()
                    .HasOne(gamePlatform => gamePlatform.Platform)
                    .WithMany()
                    .HasForeignKey(gamePlatform => gamePlatform.PlatformID)
                    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlatformModel>()
                    .HasIndex(platform => platform.ID)
                    .IsUnique()
                    .HasFilter($"{nameof(PlatformModel.ID)} IS NOT NULL");
    }
}

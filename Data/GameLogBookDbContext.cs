using Microsoft.EntityFrameworkCore;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Models.Games.Company;
using VGL.Models.Games.Platforms;
using VGL.Models.Platforms.Company;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Data;

public class GameLogBookDbContext(DbContextOptions<GameLogBookDbContext> options) 
    : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Platform> Platforms => Set<Platform>();
    
    public DbSet<Playthrough> Playthroughs => Set<Playthrough>();
    
    // Relational DBs
    public DbSet<GameCompany> GameCompanies => Set<GameCompany>();
    public DbSet<PlatformCompany> PlatformCompanies => Set<PlatformCompany>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        SetupDatabases(modelBuilder);
        SetupRelationalDbs(modelBuilder);
    }

    private void SetupDatabases(ModelBuilder modelBuilder)
    {
        SetupGameDb(modelBuilder);
        SetupPlatformDb(modelBuilder);
        SetupCompanyDb(modelBuilder);
    }

    private void SetupGameDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>().OwnsOne(game => game.Cover);
        modelBuilder.Entity<Game>().OwnsOne(game => game.Hero);
        modelBuilder.Entity<Game>().OwnsOne(game => game.Logo);
        modelBuilder.Entity<Game>().OwnsOne(game => game.Icon);
    }

    private void SetupPlatformDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Platform>().OwnsOne(platform => platform.Cover);
        modelBuilder.Entity<Platform>().OwnsOne(platform => platform.Hero);
        modelBuilder.Entity<Platform>().OwnsOne(platform => platform.Logo);
        modelBuilder.Entity<Platform>().OwnsOne(platform => platform.Icon);
    }

    private void SetupCompanyDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>().OwnsOne(company => company.Cover);
        modelBuilder.Entity<Company>().OwnsOne(company => company.Hero);
        modelBuilder.Entity<Company>().OwnsOne(company => company.Logo);
        modelBuilder.Entity<Company>().OwnsOne(company => company.Icon);
    }

    private void SetupRelationalDbs(ModelBuilder modelBuilder)
    {
        SetupGameCompanyRelationalDb(modelBuilder);
        SetupGamePlatformRelationalDb(modelBuilder);
        SetupPlatformCompanyRelationalDb(modelBuilder);
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
        modelBuilder.Entity<GamePlatformRelation>()
                    .ToTable("GamePlatform")
                    .HasKey(gpr => new
                                   {
                                       GameId = gpr.GameID,
                                       Platform = gpr.PlatformID,
                                       gpr.Ownership,
                                   });

        modelBuilder.Entity<GamePlatformRelation>()
                    .HasOne(gpr => gpr.Game)
                    .WithMany(game => game.GamePlatforms)
                    .HasForeignKey(gamePlatform => gamePlatform.GameID)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GamePlatformRelation>()
                    .HasOne(gpr => gpr.Platform)
                    .WithMany()
                    .HasForeignKey(gamePlatform => gamePlatform.PlatformID)
                    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Platform>()
                    .HasIndex(platform => platform.ID)
                    .IsUnique()
                    .HasFilter($"{nameof(Platform.ID)} IS NOT NULL");
    }

    private void SetupPlatformCompanyRelationalDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlatformCompany>()
                    .HasKey(platformCompany => new
                                               {
                                                   PlatformId = platformCompany.PlatformID,
                                                   CompanyId = platformCompany.CompanyID,
                                                   platformCompany.Role,
                                               });

        modelBuilder.Entity<PlatformCompany>()
                    .HasOne(platformCompany => platformCompany.Platform)
                    .WithMany(platform => platform.PlatformCompanies)
                    .HasForeignKey(platformCompany => platformCompany.PlatformID)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlatformCompany>()
                    .HasOne(platformCompany => platformCompany.Company)
                    .WithMany(company => company.PlatformCompanies)
                    .HasForeignKey(platformCompany => platformCompany.CompanyID)
                    .OnDelete(DeleteBehavior.Restrict);
    }
}

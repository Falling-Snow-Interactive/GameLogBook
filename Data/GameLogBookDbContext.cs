using Microsoft.EntityFrameworkCore;
using VGL.Models;
using VGL.Models.Companies;
using VGL.Models.Games;
using VGL.Models.Games.Company;
using VGL.Models.Games.Platforms;
using VGL.Models.Configuration;
using VGL.Models.Platforms.Company;
using VGL.Models.Users;
using Platform = VGL.Models.Platforms.Platform;

namespace VGL.Data;

public class GameLogBookDbContext(DbContextOptions<GameLogBookDbContext> options) 
    : DbContext(options)
{
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();

    public DbSet<Game> Games => Set<Game>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Platform> Platforms => Set<Platform>();
    
    public DbSet<Playthrough> Playthroughs => Set<Playthrough>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserGameCollection> UserGameCollections => Set<UserGameCollection>();
    public DbSet<UserPlatformCollection> UserPlatformCollections => Set<UserPlatformCollection>();
    public DbSet<UserGamePlatformOwnership> UserGamePlatformOwnerships => Set<UserGamePlatformOwnership>();
    
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
        SetupAppSettingsDb(modelBuilder);
        SetupGameDb(modelBuilder);
        SetupPlatformDb(modelBuilder);
        SetupCompanyDb(modelBuilder);
        SetupUserDb(modelBuilder);
        SetupPlaythroughDb(modelBuilder);
    }

    private static void SetupAppSettingsDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettings>()
                    .HasKey(settings => settings.ID);

        modelBuilder.Entity<AppSettings>()
                    .Property(settings => settings.ID)
                    .ValueGeneratedNever();
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

    private void SetupPlaythroughDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Playthrough>()
                    .HasOne(playthrough => playthrough.UserProfile)
                    .WithMany()
                    .HasForeignKey(playthrough => playthrough.UserProfileID)
                    .OnDelete(DeleteBehavior.Cascade);
    }

    private void SetupRelationalDbs(ModelBuilder modelBuilder)
    {
        SetupGameCompanyRelationalDb(modelBuilder);
        SetupUserGameCollectionDb(modelBuilder);
        SetupUserPlatformCollectionDb(modelBuilder);
        SetupUserGamePlatformOwnershipDb(modelBuilder);
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
    
    private void SetupUserDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>().OwnsOne(profile => profile.ProfilePicture);

        modelBuilder.Entity<Platform>()
                    .HasIndex(platform => platform.ID)
                    .IsUnique()
                    .HasFilter($"{nameof(Platform.ID)} IS NOT NULL");
    }

    private void SetupUserGameCollectionDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserGameCollection>()
                    .HasKey(userGame => new
                                        {
                                            userGame.UserProfileID,
                                            userGame.GameID
                                        });

        modelBuilder.Entity<UserGameCollection>()
                    .HasOne(userGame => userGame.UserProfile)
                    .WithMany(user => user.Games)
                    .HasForeignKey(userGame => userGame.UserProfileID)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserGameCollection>()
                    .HasOne(userGame => userGame.Game)
                    .WithMany()
                    .HasForeignKey(userGame => userGame.GameID)
                    .OnDelete(DeleteBehavior.Cascade);
    }

    private void SetupUserPlatformCollectionDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPlatformCollection>()
                    .HasKey(userPlatform => new
                                            {
                                                userPlatform.UserProfileID,
                                                userPlatform.PlatformID
                                            });

        modelBuilder.Entity<UserPlatformCollection>()
                    .HasOne(userPlatform => userPlatform.UserProfile)
                    .WithMany(user => user.Platforms)
                    .HasForeignKey(userPlatform => userPlatform.UserProfileID)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserPlatformCollection>()
                    .HasOne(userPlatform => userPlatform.Platform)
                    .WithMany()
                    .HasForeignKey(userPlatform => userPlatform.PlatformID)
                    .OnDelete(DeleteBehavior.Cascade);
    }

    private void SetupUserGamePlatformOwnershipDb(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserGamePlatformOwnership>()
                    .HasKey(ownership => new
                                         {
                                             ownership.UserProfileID,
                                             ownership.GameID,
                                             ownership.PlatformID,
                                             ownership.Ownership,
                                         });

        modelBuilder.Entity<UserGamePlatformOwnership>()
                    .HasOne(ownership => ownership.UserProfile)
                    .WithMany(user => user.GamePlatformOwnerships)
                    .HasForeignKey(ownership => ownership.UserProfileID)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserGamePlatformOwnership>()
                    .HasOne(ownership => ownership.Game)
                    .WithMany()
                    .HasForeignKey(ownership => ownership.GameID)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserGamePlatformOwnership>()
                    .HasOne(ownership => ownership.Platform)
                    .WithMany()
                    .HasForeignKey(ownership => ownership.PlatformID)
                    .OnDelete(DeleteBehavior.Cascade);
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

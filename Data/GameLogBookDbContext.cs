using GameLogBook.Models;
using GameLogBook.Models.Companies;
using GameLogBook.Models.Games;
using GameLogBook.Models.Platforms;
using Microsoft.EntityFrameworkCore;
using PlatformModel = GameLogBook.Models.Platforms.Platform;

namespace GameLogBook.Data;

public class GameLogBookDbContext(DbContextOptions<GameLogBookDbContext> options) 
    : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameCompany> GameCompanies => Set<GameCompany>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<PlatformModel> Platforms => Set<PlatformModel>();
    
    public DbSet<Playthrough> Playthroughs => Set<Playthrough>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Game>().OwnsOne(game => game.Cover);

        modelBuilder.Entity<Company>()
                    .HasIndex(company => company.IgdbId)
                    .IsUnique()
                    .HasFilter($"{nameof(Company.IgdbId)} IS NOT NULL");

        modelBuilder.Entity<GameCompany>()
                    .HasKey(gameCompany => new
                                           {
                                               gameCompany.GameId,
                                               gameCompany.CompanyId,
                                               gameCompany.Role
                                           });

        modelBuilder.Entity<GameCompany>()
                    .HasOne(gameCompany => gameCompany.Game)
                    .WithMany(game => game.Companies)
                    .HasForeignKey(gameCompany => gameCompany.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameCompany>()
                    .HasOne(gameCompany => gameCompany.Company)
                    .WithMany()
                    .HasForeignKey(gameCompany => gameCompany.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
    }
}

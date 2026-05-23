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
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<PlatformModel> Platforms => Set<PlatformModel>();
    
    public DbSet<Playthrough> Playthroughs => Set<Playthrough>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Game>().OwnsOne(game => game.Cover);
        modelBuilder.Entity<Game>().ComplexProperty(game => game.Ownership);

        modelBuilder.Entity<Company>()
                    .HasIndex(company => company.IgdbId)
                    .IsUnique()
                    .HasFilter($"{nameof(Company.IgdbId)} IS NOT NULL");
    }
}

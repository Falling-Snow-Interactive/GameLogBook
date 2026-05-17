using GameLogBook.Models;
using GameLogBook.Models.Library;
using Microsoft.EntityFrameworkCore;

namespace GameLogBook.Data;

public class GameLogBookDbContext(DbContextOptions<GameLogBookDbContext> options) 
    : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Platform> Platforms => Set<Platform>();

    // public DbSet<User> Users => Set<User>();
    // public DbSet<Playthrough> Playthroughs => Set<Playthrough>();
    // public DbSet<GameLog> Logs => Set<GameLog>();
}
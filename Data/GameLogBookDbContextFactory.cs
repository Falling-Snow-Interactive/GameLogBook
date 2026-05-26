using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VGL.Data;

public class GameLogBookDbContextFactory : IDesignTimeDbContextFactory<GameLogBookDbContext>
{
    public GameLogBookDbContext CreateDbContext(string[] args)
    {
        string databasePath = DatabasePathResolver.GetDesignTimeDatabasePath();

        DbContextOptions<GameLogBookDbContext> options = new DbContextOptionsBuilder<GameLogBookDbContext>()
                                                         .UseSqlite($"Data Source={databasePath}")
                                                         .Options;

        return new GameLogBookDbContext(options);
    }
}

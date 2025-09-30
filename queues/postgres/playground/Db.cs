using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backi.Postgres.Playground;

public class Db(DbContextOptions<Db> options) : DbContext(options)
{
    public DbSet<QueueItem> Queue { get; set; }

    public static Db Create(bool useLogger = true)
    {
        var options = new DbContextOptionsBuilder<Db>()
            .UseNpgsql("Host=localhost;Database=playground;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .UseLoggerFactory(LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddFilter("Microsoft.EntityFrameworkCore.ChangeTracking", LogLevel.Information);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.Information);
                if (useLogger)
                    builder.AddSimpleConsole(c => c.SingleLine = true);
            }))
            .Options;

        return new Db(options);
    }

    public static async Task<Db> CreateEmpty(bool useLogger = true)
    {
        var db = Create(useLogger);

        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        return db;
    }
    
    public async Task SaveThousandNewItems()
    {
        var items = Enumerable.Range(1, 1000)
            .Select(i => new QueueItem() { Name = $"Task {i}" });

        Queue.AddRange(items);

        await SaveChangesAsync();
    }

}

public class QueueItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}
using Microsoft.EntityFrameworkCore;

namespace Backi.Postgres.Playground;

[TestClass]
public class ForUpdateOperator
{
    [TestMethod]
    public async Task AllDisposed()
    {
        await (await Db.CreateEmpty(useLogger: false)).AddThousandItems();

        _ = Task.Run(async () =>
        {
            Console.WriteLine("Starting Lock Hundred");
            await using var db = Db.Create(useLogger: false);
            await using var tx = db.Database.BeginTransaction();
            _ = await db.Queue.FromSql(
                $"""
                 SELECT * 
                 FROM queue
                 LIMIT 100
                 FOR UPDATE
                 """
            ).ToListAsync();

            await FinishAndSleep();
        });
        
        await TenTimes(CheckCurrentlyAvailable);
    }
    
    [TestMethod]
    public async Task NoDisposing()
    {
        await (await Db.CreateEmpty(useLogger: false)).AddThousandItems();

        _ = Task.Run(async () =>
        {
            Console.WriteLine("Starting Lock Hundred");
            var db = Db.Create(useLogger: false);
            _ = db.Database.BeginTransaction();
            _ = await db.Queue.FromSql(
                $"""
                 SELECT * 
                 FROM queue
                 LIMIT 100
                 FOR UPDATE
                 """
            ).ToListAsync();

            await FinishAndSleep();
        });
        
        await TenTimes(CheckCurrentlyAvailable);
    }
    
    
    [TestMethod]
    public async Task OnlyDbDisposing()
    {
        await (await Db.CreateEmpty(useLogger: false)).AddThousandItems();

        _ = Task.Run(async () =>
        {
            Console.WriteLine("Starting Lock Hundred");
            await using var db = Db.Create(useLogger: false);
            _ = db.Database.BeginTransaction();
            _ = await db.Queue.FromSql(
                $"""
                 SELECT * 
                 FROM queue
                 LIMIT 100
                 FOR UPDATE
                 """
            ).ToListAsync();

            await FinishAndSleep();
        });
        
        await TenTimes(CheckCurrentlyAvailable);
    }

    [TestMethod]
    public async Task OnlyTransactionDisposing()
    {
        await (await Db.CreateEmpty(useLogger: false)).AddThousandItems();

        _ = Task.Run(async () =>
        {
            Console.WriteLine("Starting Lock Hundred");
            var db = Db.Create(useLogger: false);
            await using var tx = db.Database.BeginTransaction();
            _ = await db.Queue.FromSql(
                $"""
                 SELECT * 
                 FROM queue
                 LIMIT 100
                 FOR UPDATE
                 """
            ).ToListAsync();

            await FinishAndSleep();
        });
        
        await TenTimes(CheckCurrentlyAvailable);
    }
    
    [TestMethod]
    public async Task ExceptionInTheEnd()
    {
        await (await Db.CreateEmpty(useLogger: false)).AddThousandItems();

        _ = Task.Run(async () =>
        {
            Console.WriteLine("Starting Lock Hundred");
            var db = Db.Create(useLogger: true);
            await using var tx = db.Database.BeginTransaction();
            _ = await db.Queue.FromSql(
                $"""
                 SELECT * 
                 FROM queue
                 LIMIT 100
                 FOR UPDATE
                 """
            ).ToListAsync();

            await FinishAndSleep();
            throw new ("Unexpected expected exception");
        });
        
        await TenTimes(CheckCurrentlyAvailable);
    }
    
    public static async Task TenTimes(Func<Task> task)
    {
        var checks = 0;
        while (checks < 10)
        {
            await task();
            checks++;
        }
    }

    public async Task FinishAndSleep()
    {
        Console.WriteLine("Finished Lock Hundred Query, Sleeping");
        await Task.Delay(500);
        Console.WriteLine("Sleeping ended");
    }

    public async Task CheckCurrentlyAvailable()
    {
        await using var db = Db.Create(useLogger: false);
        await using var tx = db.Database.BeginTransaction();
        var count = await db.Database.SqlQuery<int>(
            $"""
             WITH unlocked_rows AS (
                 SELECT 1 
                 FROM queue
                 FOR UPDATE SKIP LOCKED
             )
             SELECT COUNT(*) as "Value"
             FROM unlocked_rows
             """
        ).FirstAsync();

        await Task.Delay(100);
        Console.WriteLine($"Unlocked rows count: {count}");
    }
}
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Backi.Postgres.Playground;

[TestClass]
public class TransactionDisposing
{
    [TestMethod]
    public async Task CommitChangeIfCalled()
    {
        var firstContext = await Db.CreateEmpty();
        await firstContext.InsertQueueItem();

        var secondContext = Db.Create();

        var count = await secondContext.Queue.CountAsync();
        count.ShouldBe(1);
    }
    
    [TestMethod]
    public async Task RollbackWhenDisposed()
    {
        var firstContext = await Db.CreateEmpty();
        using var tx = firstContext.Database.BeginTransactionAsync();
        await firstContext.InsertQueueItem();

        var secondContext = Db.Create();

        var count = await secondContext.Queue.CountAsync();
        count.ShouldBe(0);
    }
}

public static class InsertCommandExtension
{
    public static async Task InsertQueueItem(this Db db)
    {
        await db.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO queue
             (
                 name
             )
             VALUES
             (
                 'test x'
             )
             """);
    }
}
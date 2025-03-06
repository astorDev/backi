using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Backi.Postgres.Playground;

[TestClass]
public sealed class BasicShould
{
    [TestMethod]
    public async Task ConnectAndPerformBasicOperations()
    {
        using var db = await Db.CreateEmpty();

        db.Queue.Add(new QueueItem { Name = "Test" });
        await db.SaveChangesAsync();

        var count = await db.Queue.CountAsync();
        count.ShouldBe(1);
    }
}
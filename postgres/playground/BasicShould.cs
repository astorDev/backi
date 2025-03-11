using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Backi.Postgres.Playground;

[TestClass]
public sealed class DbShould
{
    [TestMethod]
    public async Task ConnectAndPerformBasic()
    {
        using var db = await Db.CreateEmpty(useLogger: false);
        await db.SaveThousandNewItems();

        using var db2 = Db.Create(useLogger: true);
        var count = await db2.Queue.CountAsync();
        count.ShouldBe(1_000);
    }
}
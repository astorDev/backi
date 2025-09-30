using System.Collections.Concurrent;
using MongoDB.Driver;

namespace Backi.Queues.Mongo.Playground;

[TestClass]
public class Queue
{
    public const int ExpectedCount = 1_000;
    private readonly IEnumerable<QueueRecord> data = Enumerable.Range(1, ExpectedCount).Select(i => new QueueRecord { Id = i.ToString(), Status = "pending" });

    [TestMethod]
    public async Task Naive()
    {
        var collection = new MongoClient("mongodb://localhost:27017").GetDatabase("test").GetCollection<QueueRecord>("naive");
        await collection.InsertManyAsync(data);

        var result = await RunWorkers(async processing =>
        {
            var records = await collection.Find(x => x.Status == "pending")
                .Limit(100)
                .ToListAsync();

            while (records.Count > 0)
            {
                var updated = await collection.UpdateManyAsync(
                    Builders<QueueRecord>.Filter.In(r => r.Id, records.Select(r => r.Id)),
                    Builders<QueueRecord>.Update.Set(r => r.Status, "processing")
                );

                await Task.WhenAll(records.Select(r => processing(r.Id)));

                await collection.UpdateManyAsync(
                    Builders<QueueRecord>.Filter.In(r => r.Id, records.Select(r => r.Id)),
                    Builders<QueueRecord>.Update.Set(r => r.Status, "done")
                );

                records = await collection.Find(x => x.Status == "pending")
                    .Limit(100)
                    .ToListAsync();
            }
        });

        Console.WriteLine($"Processed {result.Count} items");
        result.Count.ShouldBe(ExpectedCount);
    }

    [TestMethod]
    public async Task Concurrent()
    {
        var collection = new MongoClient("mongodb://localhost:27017").GetDatabase("test").GetCollection<QueueRecord>("concurrent");
        await collection.InsertManyAsync(data);

        var result = await RunWorkers(async processing =>
        {
            var record = await collection.FindOneAndUpdateAsync(
                    Builders<QueueRecord>.Filter.Eq(r => r.Status, "pending"),
                    Builders<QueueRecord>.Update.Set(r => r.Status, "processing"));

            while (record != null)
            {
                await processing(record.Id);

                await collection.UpdateOneAsync(
                    Builders<QueueRecord>.Filter.Eq(r => r.Id, record.Id),
                    Builders<QueueRecord>.Update.Set(r => r.Status, "done"));

                record = await collection.FindOneAndUpdateAsync(
                    Builders<QueueRecord>.Filter.Eq(r => r.Status, "pending"),
                    Builders<QueueRecord>.Update.Set(r => r.Status, "processing"));
            }
        });

        Console.WriteLine($"Processed {result.Count} items");
        result.Count.ShouldBe(ExpectedCount);
    }

    public async Task<ConcurrentBag<string>> RunWorkers(Func<Func<string, Task>, Task> workerFactory)
    {
        var processed = new ConcurrentBag<string>();
        Func<string, Task> processing = (x) => Task.Delay(5).ContinueWith(_ => processed.Add(x));

        var workers = Enumerable.Range(1, 10).Select(_ => workerFactory(processing)).ToArray();
        await Task.WhenAll(workers);
        return processed;
    }
}

public class QueueRecord
{
    public required string Id { get; set; } = null!;
    public required string Status { get; set; } = null!;
}
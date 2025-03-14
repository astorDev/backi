# PostgreSQL Queue Processing: How to Handle Concurrency Efficiently

In modern applications, efficient queue processing is crucial for maintaining performance and ensuring that tasks are handled in a timely manner. When dealing with high volumes of tasks, managing concurrency becomes a key challenge. PostgreSQL, with its robust feature set, provides powerful tools to handle these challenges effectively.

In this article, we will explore how to manage concurrency in PostgreSQL queue processing. We will provide practical examples using C# and Entity Framework, demonstrating how to implement efficient and reliable queue processing systems. Whether you are building a new application or optimizing an existing one, these techniques will help you ensure that your queue processing is both performant and concurrency-friendly.

Let's dive in and see how you can leverage PostgreSQL and .NET to handle queue processing with ease.

## Building the Playground: Deploying PostgreSQL via Docker and Connecting to it using Entity Framework Core

Let's start by deploying PostgreSQL on our localhost. Here's a `compose.yml` file that does just that:

```yaml
services:
  postgres:
    image: postgres:13
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
```

After running `docker compose up -d` we should have our database deployed. Let's also create a new .NET project by running `dotnet new console`. Finally, we'll need a package for PostgreSQL provider for Entity Framework, a package to use `snake_case` naming convention, and a console logger, for the observability. Here's the script to install all of them:

```sh
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package EFCore.NamingConventions
dotnet add package Microsoft.Extensions.Logging.Console
```

Now, let's make a very simple context with just a single table:

```csharp
public class Db(DbContextOptions<Db> options) : DbContext(options)
{
    public DbSet<QueueItem> Queue { get; set; }
}

public class QueueItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}
```

Let's also do a very **not** production-ready helper method for initializing our database using the packages we've installed earlier:

```csharp
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
```

Let's also do another helper method that will allow us to instantly get a brand new database based on the schema we've described using our entity objects:

```csharp
public static async Task<Db> CreateEmpty(bool useLogger = true)
{
    var db = Create(useLogger);

    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();

    return db;
}
```

The next thing we'll need is a thousand queue items for us to play around with. Let's add a helper method for them:

```csharp
public async Task SaveThousandNewItems()
{
    var items = Enumerable.Range(1, 1000)
        .Select(i => new QueueItem() { Name = $"Task {i}" });

    Queue.AddRange(items);

    await SaveChangesAsync();
}
```

Before moving to the interesting part, let's test what we have here:

```csharp
using var db = await Db.CreateEmpty(useLogger: false);
await db.SaveThousandNewItems();

using var db2 = Db.Create(useLogger: true);
var count = await db2.Queue.CountAsync(); // should return 1000
```

I hope you'll also be able to successfully obtain a 1000, because now it's time to do some queue processing!

## Introducing the FOR UPDATE Operator!

As you may see in the title, we'll use the `FOR UPDATE` operator for our task. The operator locks selected rows for the duration of the current transaction, which is exactly what we need: Exclusively acquire a set of queue items for the time of processing. Our goal for this article is just to get familiar with the basics of the operator, so we'll use the simplest example possible:

```sql
SELECT * 
FROM queue
LIMIT 100
FOR UPDATE
```

Locking the rows is good for exclusivity, but pretty bad for performance, since the other queries will likely just wait for the transaction to release the lock. Gladly, we can tell other queries to just forget about the locked rows using `FOR UPDATE SKIP LOCKED` construct. Here's how we can use this operator in another query, calculating how many rows are currently available:

```sql
WITH unlocked_rows AS (
    SELECT 1 
    FROM queue
    FOR UPDATE SKIP LOCKED
)
SELECT COUNT(*) as "Value"
FROM unlocked_rows
```

Now, when we know our SQL, let's put it together with Entity Framework and run some experiments!

## Locking a Hundred Items in EF with the FOR UPDATE operator

First thing first, we'll initiate our database

```csharp
await (await Db.CreateEmpty(useLogger: false)).SaveThousandNewItems();
```

Then, using a new `db` instance we'll lock a hundred items. We'll need to begin a transaction, so that our retrieved rows will stay locked not just for the duration of the query, but until we close the transaction. Here's how we can start the query:

```csharp
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
```

Before exiting let's sleep for a little while, so that we can run another query against the table with some locked items:

```csharp
public async Task FinishAndSleep(int milliseconds)
{
    Console.WriteLine("Finished Lock Hundred Query, Sleeping");
    await Task.Delay(milliseconds);
    Console.WriteLine("Sleeping ended");
}
```

What we'll run in another thread is our familiar query, counting currently available items. Here's the code:

```csharp
public async Task CheckCurrentlyAvailable(int milliseconds)
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

    await Task.Delay(milliseconds);
    Console.WriteLine($"Unlocked rows count: {count}");
}
```

Moreover, we won't just count it once, we'll do it 10 times to track dynamics. Here's a helper method, that will let us achieve that;

```csharp
public static async Task TenTimes(Func<Task> task)
{
    var checks = 0;
    while (checks < 10)
    {
        await task();
        checks++;
    }
}
```

Let's put it all together! After initiating the database, we'll start a task in a background, locking our items for about 500 milliseconds. In parallel, we'll run the check of currently available items. Here's how it all will look together:

```csharp
await (await Db.CreateEmpty(useLogger: false)).SaveThousandNewItems();

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

    await FinishAndSleep(500);
});
        
await TenTimes(() => CheckCurrentlyAvailable(100));
```

And here's the result we might expect:

```text
 Starting Lock Hundred
 Finished Lock Hundred Query, Sleeping
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Sleeping ended
 Unlocked rows count: 900
 Unlocked rows count: 1000
 Unlocked rows count: 1000
 Unlocked rows count: 1000
 Unlocked rows count: 1000
 Unlocked rows count: 1000
```

As you may see, the query successfully locked a hundred items for the first five check iterations. After that, we were able to get the whole thousand items unlocked!

This is basically it about the PostgreSQL! But if you are anything like me, you might be wondering how the transactions we've used are closed in EF - let's experiment with that!

## Bonus: Playing around with Entity Framework (EF) Transactions

First, let's try removing all the `using` statements we have:

```csharp
var db = Db.Create(useLogger: false);
_ = db.Database.BeginTransaction();
```

Here's what we will get in this case:

```text
 Starting Lock Hundred
 Unlocked rows count: 1000
 Finished Lock Hundred Query, Sleeping
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Sleeping ended
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
```

As you might see, the lock never ended, since we've never disposed the transaction in any way. But what will happen if we dispose only the `Db` and not the transaction:

```csharp
await using var db = Db.Create(useLogger: false);
_ = db.Database.BeginTransaction();
```

Well, it seems like this will still unlock our rows:

```text
 Starting Lock Hundred
 Finished Lock Hundred Query, Sleeping
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Sleeping ended
 Unlocked rows count: 900
 Unlocked rows count: 1000
 Unlocked rows count: 1000
 Unlocked rows count: 1000
 Unlocked rows count: 1000
 Unlocked rows count: 1000
```

And what if we dispose just the transaction:

```csharp
var db = Db.Create(useLogger: false);
await using var tx = db.Database.BeginTransaction();
```

As you might expect, that will work as well:

```text
 Starting Lock Hundred
 Unlocked rows count: 1000
 Finished Lock Hundred Query, Sleeping
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Unlocked rows count: 900
 Sleeping ended
 Unlocked rows count: 900
 Unlocked rows count: 1000
 Unlocked rows count: 1000
 Unlocked rows count: 1000
 Unlocked rows count: 1000
```

The conclusion that we might end up with is: don't forget to dispose... at least something.

## Wrapping Up!

By following the techniques and examples provided in this article, you should now have a solid understanding of how to handle concurrency in PostgreSQL queue processing using C# and Entity Framework. Efficient queue processing is essential for maintaining the performance and reliability of your applications, especially when dealing with high volumes of tasks.

You can find the code from this article [here in the backi GitHub repository](https://github.com/astorDev/backi/blob/main/postgres/playground/ForUpdateOperator.cs). The repository contains tools and best practices for various background processing operations like the one we've discussed in this article. Don't hesitate to give this repository a star! ⭐

And also ... claps are appreciated! 👏

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

## Locking a Thousand Items utilizing the FOR UPDATE operator

`FOR UPDATE`

```sql
SELECT * 
FROM queue
LIMIT 100
FOR UPDATE
```

```csharp
await (await Db.CreateEmpty(useLogger: false)).SaveThousandNewItems();
```

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


```csharp
public async Task FinishAndSleep(int milliseconds)
{
    Console.WriteLine("Finished Lock Hundred Query, Sleeping");
    await Task.Delay(milliseconds);
    Console.WriteLine("Sleeping ended");
}
```

`FOR UPDATE SKIP LOCKED`

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

## Playing around with Entity Framework (EF) Transactions

```csharp
var db = Db.Create(useLogger: false);
_ = db.Database.BeginTransaction();
```

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

```csharp
await using var db = Db.Create(useLogger: false);
_ = db.Database.BeginTransaction();
```

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

```csharp
var db = Db.Create(useLogger: false);
await using var tx = db.Database.BeginTransaction();
```

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

## Wrapping Up!

By following the techniques and examples provided in this article, you should now have a solid understanding of how to handle concurrency in PostgreSQL queue processing using C# and Entity Framework. Efficient queue processing is essential for maintaining the performance and reliability of your applications, especially when dealing with high volumes of tasks.

You can find the code from this article [here in the backi GitHub repository](https://github.com/astorDev/backi/blob/main/postgres/playground/ForUpdateOperator.cs). The repository contains tools and best practices for various background processing operations like the one we've discussed in this article. Don't hesitate to give this repository a star! ⭐

And also ... claps are appreciated! 👏

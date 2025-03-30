## How to Implement a .NET Background Service for Continuous Work in 2025

> Implementing an Ever-Running Background Service utilizing C# 11 feature - static virtual members in interfaces

.NET provides us with a built-in way to create a background service via `IHostedService` and its specific implementation - `BackgroundService`. However, there's not much infrastructure provided for common scenarios, so that falls on our shoulders. In [this article](https://medium.com/@vosarat1995/net-timers-all-you-need-to-know-d020c73b63a4), I've shown how we can run a periodic job using timers. In this article, we'll build a service that runs the same short operation safely on repeat for the life scope of our application.

> Or jump straight to the [TLDR;](#tldr) in the end of this article

## TLDR;

In this article, we've implemented a base infrastructure for a background service running the same operation on repeat. Instead of implementing it again, you can just install the `Backi.Continuous` package:

```sh
dotnet add package Backi.Continuous
```

Then, implement your own `IContinuousWorkIteration` like this:

```csharp
public class MyIteration(ILogger<MyIteration> logger) : IContinuousWorkIteration
{
    public static async Task OnException(Exception ex, ILogger logger)
    {
        logger.LogError(ex, "An error occurred");
        await Task.Delay(500);
    }
    
    public async Task Run(CancellationToken stoppingToken)
    {
        logger.LogInformation("Running");
        await Task.Delay(1000);
        logger.LogInformation("Done");
    }
}
```

Finally, attach the continuous background service to the DI container:

```csharp
services.AddContinuousBackgroundService<MyIteration>()
```

Now, when running the application, you should get a log looking something like this:

![](demo.png)

And that wraps up our continuous background service infrastructure. This article, along with the `Backi.Continuous` package, is part of the bigger [backi](https://github.com/astorDev/backi) project, helping with various background-related stuff. Check it out on the [GitHub](https://github.com/astorDev/backi) and don't hesitate to give it a star! ⭐

Claps for this article are also highly appreciated! 😉

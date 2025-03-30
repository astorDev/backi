## How to Implement a .NET Background Service for Continuous Work in 2025

> Implementing an Ever-Running Background Service utilizing C# 11 feature - static virtual members in interfaces

.NET Provides us with a built-in way to create a Background Service via `IHostedService` and it's specific implementation - `BackgroundService`. However, there's not much infrastructure provided for common scenarios, so that lays on our sholder. In [this article](https://medium.com/@vosarat1995/net-timers-all-you-need-to-know-d020c73b63a4), I've shown how we can run a periodic job using timers. In this article, we'll build a service, that runs the same short operation safely on repeat for the life scope of our application.
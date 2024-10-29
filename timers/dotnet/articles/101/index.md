# .NET Timers: All You Need to Know

Periodically executing an action is pretty common programming task, virtually any .NET developer will face sooner or later.In fact, the task is so common that .NET ships 3! different timers (not counting UI-specific timers). Sadly, Microsoft doesn't really provide a guide on which one to choose and how to use it in a real-world application. So let's experiment on our own!

## Recap

Since the sixth version .NET ships with 3 timers: `System.Threading.Timer`, `System.Timers.Timer`, and `System.Threading.PeriodicTimer`. We've experimented with them in our `HostedTimerService` and figured out API and behaviour of each. Unfortunatelly, none of them comes with a simple API or exception handling. So we have created our own version on top of the `System.Threading.Timer`.

You can use the timer as a [nuget](https://www.nuget.org/packages/Backi.Timers) or check out the source code in [the github repo](). And by the way... claps are appreciated 👏
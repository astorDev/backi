# Self-Calls

```csharp
builder.Services.AddPeriodicSelfCall(TimeSpan.FromSeconds(10), '/maintenance');
builder.Services.AddPeriodicAlertSelfCall();

var app = builder.Build();

app.MapPost('/maintenance', (IService service) => {
    service.PerformMaintenance();
});

app.MapPostAlerts();
```

`Alerts.cs`:

```csharp
public static class AlertEndpoints
{
    public static void MapPostAlerts()
    {
        app.MapPost(Uris.Alerts, (IAlert alert) => alerts.Do());
    }
}

public static class AlertSelfCall
{
    public static void AddPeriodicAlertSelfCall(this IServiceCollection services)
    {
        services.AddPeriodicaSelfCall("Alerts:Interval", Uris.Alerts);
    }
}
```
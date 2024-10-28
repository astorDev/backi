using Backi.Timers;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(c => c.SingleLine = true);

builder.Services.AddSingleton<SingletonWithSystemTimer>();
builder.Services.AddSingleton<SingletonWithThreadingTimer>();
builder.Services.AddSingleton<SingletonWithPeriodicTimer>();
builder.Services.AddSingleton<SingletonWithSafeTimer>();

builder.Services.AddHostedService<HostedTimerService>();

var app = builder.Build();

var timer = SafeTimer.Unstarted(
    () => app.Logger.LogInformation("Program timer ticked. {time:O}", DateTime.Now)
);

app.MapGet("/start", () => {
    timer.Start(TimeSpan.FromSeconds(1));
    return "Started!";
});

app.MapGet("/stop", () => {
    timer.Stop();
    return "Stopped!";
});

app.MapGet("/system", (SingletonWithSystemTimer _) => "Triggered System Timer!");
app.MapGet("/threading", (SingletonWithThreadingTimer _) => "Triggered Threading Timer!");
app.MapGet("/periodic", (SingletonWithThreadingTimer _) => "Triggered Threading Timer!");
app.MapGet("/safe", (SingletonWithSafeTimer _) => "Triggered Threading Timer!");

app.Run();
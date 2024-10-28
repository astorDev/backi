using Backi.Timers;

public class HostedTimerService(IConfiguration configuration, ILogger<HostedTimerService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var timerType = configuration["Timer"];
        
        logger.LogInformation("Starting with timer {timerType}", timerType);

        Action startAction = timerType switch
        {
            "Threading" => StartThreadingTimer,
            "Periodic" => StartPeriodicTimer,
            "Safe" => StartSafeTimer,
            "System" => StartSystemTimer,
            _ => () => { },
        };

        startAction();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("HostedTimerService stopped");
        return Task.CompletedTask;
    }

    public void StartSystemTimer()
    {
        var timer = new System.Timers.Timer(TimeSpan.FromSeconds(2));
        timer.Elapsed += (sender, e) => Tick();
        timer.Start();
    }

    public void StartThreadingTimer()
    {
        _ = new Timer((t) => Tick(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    public void StartPeriodicTimer()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        Task.Run(async () =>
        {
            while (await timer.WaitForNextTickAsync())
            {
                Tick();
            }
        });
    }

    public void StartSafeTimer() {
        _ = SafeTimer.RunNowAndPeriodically(TimeSpan.FromSeconds(2), Tick, ex => logger.LogError(ex, "Error in timer"));
    }

    public void Tick()
    {
        var logTickError = configuration.GetValue("LogTickError", false);

        logger.LogInformation("Ticked at {time}", DateTime.Now);
        if (DateTime.Now.Second % 3 == 0)
        {
            if (logTickError) logger.LogError("Ticked at the wrong time");
            throw new("Ticked at the wrong time");
        }
    }
}
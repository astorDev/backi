
public class HostedServiceWithTimer(ILogger<HostedServiceWithTimer> logger) : IHostedService
{
    Timer timer = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(
            (t) =>
            {
                logger.LogInformation("ticked");
                if (DateTime.Now.Second % 3 == 0)
                {
                    logger.LogError("Ticked at the wrong time");
                    throw new Exception("Ticked at the wrong time");
                }
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2)
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer.Dispose();

        return Task.CompletedTask;
    }
}
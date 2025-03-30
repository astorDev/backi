namespace V1;

public interface IContinuousWorkIteration
{
    Task Run(CancellationToken stoppingToken);
}

public class ContinuousBackgroundService<TIteration>(TIteration iteration) 
    : BackgroundService 
    where TIteration : IContinuousWorkIteration
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await iteration.Run(stoppingToken);
        }
    }
}

public static partial class Registration
{
    public static IServiceCollection AddContinuousBackgroundService<TIteration>(this IServiceCollection services)
        where TIteration : class, IContinuousWorkIteration
    {
        services.AddSingleton<TIteration>();
        services.AddHostedService<ContinuousBackgroundService<TIteration>>();
        return services;
    }
}
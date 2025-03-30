namespace V2;

public interface IContinuousWorkIteration
{
    Task Run(CancellationToken stoppingToken);
}

public class ContinuousBackgroundService<TIteration>(IServiceScopeFactory scopeFactory) 
    : BackgroundService 
    where TIteration : IContinuousWorkIteration
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var iteration = scope.ServiceProvider.GetRequiredService<TIteration>();

            await iteration.Run(stoppingToken);
        }
    }
}

public static partial class Registration
{
    public static IServiceCollection AddContinuousBackgroundService<TIteration>(this IServiceCollection services)
        where TIteration : class, IContinuousWorkIteration
    {
        services.AddScoped<TIteration>();
        services.AddHostedService<ContinuousBackgroundService<TIteration>>();
        return services;
    }
}
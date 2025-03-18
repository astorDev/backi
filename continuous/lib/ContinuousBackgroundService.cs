namespace Backi;

public interface IContinuousWorkIteration
{
    Task Run(CancellationToken stoppingToken);
    abstract static Task OnException(Exception ex, ILogger logger);
}

public class ContinuousBackgroundService<TIteration>(IServiceScopeFactory scopeFactory, ILogger<TIteration> logger) 
    : BackgroundService 
    where TIteration : IContinuousWorkIteration
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var iteration = scope.ServiceProvider.GetRequiredService<TIteration>();
                
                await iteration.Run(stoppingToken);
            }
            catch (Exception ex)
            {
                await TIteration.OnException(ex, logger);
            }
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
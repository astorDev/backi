using Backi.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backi;

public class TimediatrBackgroundService(
    IOptions<TimediatrConfiguration> options,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TimediatrBackgroundService> logger) : IHostedService
{
    readonly List<SafeTimer> timers = [];
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var schedule = options.Value.Schedule;

        foreach (var timer in schedule)
        {
            logger.LogDebug("Setting {requestType} to run with interval {interval}", timer.Key.GetType(), timer.Value);
            
            timers.Add(SafeTimer.RunNowAndPeriodically(
                timer.Value,
                () => serviceScopeFactory.SendMediatorRequest(timer.Key, logger, cancellationToken),
                ex => logger.LogError(ex, "Error while processing {requestType}", timer.Key.GetType())
            ));
            
            logger.LogInformation("Set {requestType} to run with interval {interval}", timer.Key.GetType(), timer.Value);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Stopping all timers");
        
        foreach (var timer in timers)
        {
            timer.Stop();
        }
        
        logger.LogInformation("Stopped all timers");
        return Task.CompletedTask;
    }
}

public class TimediatrConfiguration
{
    public Dictionary<object, TimeSpan> Schedule { get; set; } = new();
}

public static class TimediatrServiceCollectionExtensions
{
    public static IServiceCollection AddTimediatr(this IServiceCollection services, Action<OptionsBuilder<TimediatrConfiguration>> configuration)
    {
        services.AddHostedService<TimediatrBackgroundService>();
        var optionsBuilder = services.AddOptions<TimediatrConfiguration>();
        configuration(optionsBuilder);

        return services;
    }
}
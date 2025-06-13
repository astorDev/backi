using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backi.Tests;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediatrTestingInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<CounterCollection>();
        services.AddMediatR(m => m.RegisterServicesFromAssembly(typeof(CounterCollection).Assembly));
        services.AddLogging(l =>
        {
            l.AddSimpleConsole(c => c.SingleLine = true);
            l.SetMinimumLevel(LogLevel.Debug);
        });

        return services;
    }
}

public class CounterCollection
{
    public readonly Dictionary<string, int> items = new ();

    public void Increment(string key)
    {
        if (!items.TryAdd(key, 1))
        {
            items[key] += 1;
        }
    }

    public int Get(string key) => items.GetValueOrDefault(key);
}

public class CommandOne : IRequest
{
    public class Handler : IRequestHandler<CommandOne>
    {
        readonly CounterCollection counters;
        public const string ConstructorCounterKey = "CommandOne.Handler.Constructor";
        public const string HandleCounterKey = "CommandOne.Handler.Handle";
        
        public Handler(CounterCollection counters)
        {
            this.counters = counters;
            
            counters.Increment(ConstructorCounterKey);
        }

        public Task Handle(CommandOne request, CancellationToken cancellationToken)
        {
            counters.Increment(HandleCounterKey);
            return Task.CompletedTask;
        }
    }
}

public class CommandTwo : IRequest
{
    public class Handler : IRequestHandler<CommandTwo>
    {
        readonly CounterCollection counters;
        public const string ConstructorCounterKey = "CommandTwo.Handler.Constructor";
        public const string HandleCounterKey = "CommandTwo.Handler.Handle";
        
        public Handler(CounterCollection counters)
        {
            this.counters = counters;
            
            counters.Increment(ConstructorCounterKey);
        }

        public Task Handle(CommandTwo request, CancellationToken cancellationToken)
        {
            counters.Increment(HandleCounterKey);
            return Task.CompletedTask;
        }
    }
}
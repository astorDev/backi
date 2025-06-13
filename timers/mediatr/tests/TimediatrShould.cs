using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Backi.Tests;

[TestClass]
public class TimediatrShould
{
    [TestMethod]
    public async Task SendCommandOneFewTimes()
    {
        var services = new ServiceCollection();

        services.AddMediatrTestingInfrastructure();
        services.AddTimediatr(timediatr => timediatr.Configure(o =>
        {
            o.Schedule.Add(new CommandOne(), TimeSpan.FromSeconds(3));
        }));

        var provider = services.BuildServiceProvider();
        
        var backgroundService = (TimediatrBackgroundService)provider.GetRequiredService<IHostedService>();
        
        await backgroundService.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(10));
        await backgroundService.StopAsync(CancellationToken.None);

        var counter = provider.GetRequiredService<CounterCollection>();

        counter.Get(CommandOne.Handler.HandleCounterKey).Should().Be(4);
        counter.Get(CommandOne.Handler.ConstructorCounterKey).Should().Be(4);
    }
    
    [TestMethod]
    public async Task SendMultipleCommandsFewTimes()
    {
        var services = new ServiceCollection();

        services.AddMediatrTestingInfrastructure();
        services.AddTimediatr(timediatr => timediatr.Configure(o =>
        {
            o.Schedule.Add(new CommandOne(), TimeSpan.FromSeconds(3));
            o.Schedule.Add(new CommandTwo(), TimeSpan.FromSeconds(4));
        }));

        var provider = services.BuildServiceProvider();
        
        var backgroundService = (TimediatrBackgroundService)provider.GetRequiredService<IHostedService>();
        
        await backgroundService.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(10));
        await backgroundService.StopAsync(CancellationToken.None);

        var counter = provider.GetRequiredService<CounterCollection>();

        counter.Get(CommandOne.Handler.HandleCounterKey).Should().Be(4);
        counter.Get(CommandOne.Handler.ConstructorCounterKey).Should().Be(4);
        counter.Get(CommandTwo.Handler.HandleCounterKey).Should().Be(3);
        counter.Get(CommandTwo.Handler.ConstructorCounterKey).Should().Be(3);
    }
    
    [TestMethod]
    public async Task SendMultipleCommandsFromConfig()
    {
        var services = new ServiceCollection();
        
        services.AddMediatrTestingInfrastructure();

        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("Timers:Backi.Tests.CommandOne", "00:00:03"),
            new KeyValuePair<string, string?>("Timers:Backi.Tests.CommandTwo", "00:00:04"),
        });
        
        services.AddTimediatr(timediatr => timediatr.Configure(o =>
        {
            o.AddAllFrom(configuration.GetSection("Timers"), typeof(CounterCollection).Assembly);
        }));

        var provider = services.BuildServiceProvider();
        
        var backgroundService = (TimediatrBackgroundService)provider.GetRequiredService<IHostedService>();
        
        await backgroundService.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(10));
        await backgroundService.StopAsync(CancellationToken.None);

        var counter = provider.GetRequiredService<CounterCollection>();

        counter.Get(CommandOne.Handler.HandleCounterKey).Should().Be(4);
        counter.Get(CommandOne.Handler.ConstructorCounterKey).Should().Be(4);
        counter.Get(CommandTwo.Handler.HandleCounterKey).Should().Be(3);
        counter.Get(CommandTwo.Handler.ConstructorCounterKey).Should().Be(3);
    }
}
using FluentAssertions;
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
}
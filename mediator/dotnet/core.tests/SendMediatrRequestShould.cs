using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Backi;

namespace Backi.Tests;

[TestClass]
public class SendMediatrRequestShould
{
    [TestMethod]
    public async Task CreateHandlerEachTime()
    {
        var services = new ServiceCollection();
        
        services.AddMediatrTestingInfrastructure();
        
        var provider = services.BuildServiceProvider();
        
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var logger = provider.GetRequiredService<ILogger<SendMediatrRequestShould>>();
        
        var commandInstance = new CommandOne();
        
        await scopeFactory.SendMediatorRequest(commandInstance, logger);
        await scopeFactory.SendMediatorRequest(commandInstance, logger);

        var counters = provider.GetRequiredService<CounterCollection>();

        counters.Get(CommandOne.Handler.ConstructorCounterKey).Should().Be(2);
        counters.Get(CommandOne.Handler.HandleCounterKey).Should().Be(2);
    }
}
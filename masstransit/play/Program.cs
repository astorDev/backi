using Backi;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(c => c.SingleLine = true);

builder.Services.AddHostedService<SayHelloAsker>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SayHelloConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapGet("/", () => new {
    Message = "Hello World!"
});

app.Run();

namespace Backi
{
    public class SayHello
    {
        public required string Name { get; set; }
    }
    
    public class SayHelloConsumer(ILogger<SayHelloConsumer> logger) : IConsumer<SayHello>
    {
        public Task Consume(ConsumeContext<SayHello> context)
        {
            logger.LogInformation("Hello, {Name}!", context.Message.Name);
            return Task.CompletedTask;
        }
    }
    
    public class SayHelloAsker(IBus bus) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await bus.Publish(new SayHello { Name = "MassTransit" }, stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}


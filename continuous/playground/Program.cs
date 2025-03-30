using Backi;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(c => c.SingleLine = true);

builder.Services.AddContinuousBackgroundService<MyIteration>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();

public class MyIteration(ILogger<MyIteration> logger) : IContinuousWorkIteration
{
    public static async Task OnException(Exception ex, ILogger logger)
    {
        logger.LogError(ex, "An error occurred");
        await Task.Delay(500);
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        logger.LogInformation("Running");
        await Task.Delay(1000);
        logger.LogInformation("Done");
    }
}
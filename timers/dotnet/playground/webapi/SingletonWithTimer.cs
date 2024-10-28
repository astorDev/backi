using Backi.Timers;

public class SingletonWithSystemTimer
{
    public SingletonWithSystemTimer(ILogger<SingletonWithSystemTimer> logger)
    {
        logger.LogInformation("created");
        
        var timer = new System.Timers.Timer(TimeSpan.FromSeconds(2));
        timer.Elapsed += (sender, e) => {
            logger.LogInformation("ticked");
            if (DateTime.Now.Second % 3 == 0)
            {
                logger.LogError("Ticked at the wrong time");
                throw new Exception("Ticked at the wrong time");
            }
        };
    
        timer.Start();
    }
}

public class SingletonWithThreadingTimer
{
    public SingletonWithThreadingTimer(ILogger<SingletonWithThreadingTimer> logger)
    {
        logger.LogInformation("created");
        
        var timer = new System.Threading.Timer(
            (t) => {
                logger.LogInformation("ticked");
                if (DateTime.Now.Second % 3 == 0)
                {
                    logger.LogError("Ticked at the wrong time");
                    throw new Exception("Ticked at the wrong time");
                }
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2)
        );
    }
}

public class SingletonWithPeriodicTimer
{
    public SingletonWithPeriodicTimer(ILogger<SingletonWithPeriodicTimer> logger)
    {
        logger.LogInformation("created");
        
        var timer = new System.Threading.PeriodicTimer(TimeSpan.FromSeconds(2));
        Task.Run(async () => {
            while (await timer.WaitForNextTickAsync())
            {
                logger.LogInformation("ticked");
                if (DateTime.Now.Second % 3 == 0)
                {
                    logger.LogError("Ticked at the wrong time");
                    throw new Exception("Ticked at the wrong time");
                }
            }
        });
    }
}

public class SingletonWithSafeTimer
{
    public SingletonWithSafeTimer(ILogger<SingletonWithSafeTimer> logger)
    {
        logger.LogInformation("created");

        _ = SafeTimer.RunNowAndPeriodically(
            TimeSpan.FromSeconds(2),
            () => {
                logger.LogInformation("ticked");
                if (DateTime.Now.Second % 3 == 0)
                {
                    logger.LogError("Ticked at the wrong time");
                }
            },
            (ex) => {
                logger.LogError(ex, "Error in timer");
            }
        );
    }
}
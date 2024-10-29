namespace Backi.Timers.Tests;

[TestClass]
public class SafeTimerShould
{
    [TestMethod]
    public async Task AcceptAsyncOperations()
    {
        SafeTimer.RunNowAndPeriodically(
            TimeSpan.FromSeconds(2),
            async () =>
            {
                await Task.Delay(100);
                Console.WriteLine($"Ticked at {DateTime.Now:O}");
                if (DateTime.Now.Second % 3 == 0)
                {
                    throw new InvalidOperationException("Ticked at the wrong time");
                }
            },
            ex =>
            {
                Console.WriteLine($"Caught exception: {ex.Message}");
            });

        await Task.Delay(TimeSpan.FromSeconds(10));
    }
}
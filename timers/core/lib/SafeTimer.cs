namespace Backi.Timers;

public class SafeTimer(Timer innerTimer)
{
    public void Stop() {
        innerTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Start(TimeSpan interval) {
        innerTimer.Change(TimeSpan.Zero, interval);
    }
    
    public static SafeTimer Unstarted(Action action, Action<Exception>? onException = null) {
        return Unstarted(TimerCallback(action, onException));
    }
    
    public static SafeTimer Unstarted(Func<Task> action, Action<Exception>? onException = null) {
        return Unstarted(TimerCallback(action, onException));
    }
    
    public static SafeTimer RunNowAndPeriodically(TimeSpan interval, Action action, Action<Exception>? onException = null) {
        return RunNowAndPeriodically(TimerCallback(action, onException), interval);
    }
    
    public static SafeTimer RunNowAndPeriodically(TimeSpan interval, Func<Task> action, Action<Exception>? onException = null) {
        return RunNowAndPeriodically(TimerCallback(action, onException), interval);
    }
    
    private static SafeTimer RunNowAndPeriodically(TimerCallback callback, TimeSpan interval)
    {
        var innerTimer = new Timer(callback, null, TimeSpan.Zero, interval);
        return new(innerTimer);
    }
    
    private static SafeTimer Unstarted(TimerCallback callback)
    {
        var innerTimer = new Timer(callback, null, Timeout.Infinite, Timeout.Infinite);
        return new(innerTimer);
    }
    
    private static TimerCallback TimerCallback(Action action, Action<Exception>? onException = null)
    {
        return (_) =>
        {
            try {
                action();
            }
            catch (Exception ex) {
                onException?.Invoke(ex);
            }
        };
    }

    private static TimerCallback TimerCallback(Func<Task> action, Action<Exception>? onException = null)
    {
        return async (_) =>
        {
            try {
                await action();
            }
            catch (Exception ex) {
                onException?.Invoke(ex);
            }
        };
    }
}
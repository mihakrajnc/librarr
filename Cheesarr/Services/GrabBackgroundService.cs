namespace Cheesarr.Services;

public class GrabBackgroundService(GrabStateUpdater stateUpdater) : BackgroundService
{
    private int _number = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _number++;
            stateUpdater.OnNumberUpdated(_number);
            await Task.Delay(3000, stoppingToken);
        }
    }
}

public class GrabStateUpdater
{
    public event EventHandler<int>? NumberChanged;

    public void OnNumberUpdated(int number)
    {
        NumberChanged?.Invoke(this, number);
    }
}
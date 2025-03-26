namespace Librarr.Services.Jobs;

/// <summary>
/// Background service that schedules and runs jobs concurrently.
/// </summary>
public class JobsBackgroundService(
    IEnumerable<IJob> jobs,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    ILogger<JobsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait until the application signals it has fully started.
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using (lifetime.ApplicationStarted.Register(() => tcs.TrySetResult()))
        {
            await tcs.Task;
        }

        // Start a task for each job and run them concurrently.
        logger.LogInformation("Jobs background service started");
        await Task.WhenAll(jobs.Select(job => RunJobAsync(job, stoppingToken)));
    }

    // Helper method that runs each job in a loop.
    private async Task RunJobAsync(IJob job, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // TODO: Maybe create a new instance of a job on each iteration?
                logger.LogInformation("Running job: {name}", job.GetType().Name);
                using var scope = scopeFactory.CreateScope();
                await job.ExecuteAsync(scope, cancellationToken);
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed.
                logger.LogError(ex, "Error executing job {name}", job.GetType().Name);
            }

            // Wait for the specified delay before running again.
            try
            {
                await Task.Delay(job.Interval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Task was canceled; exit gracefully.
            }
        }
    }
}
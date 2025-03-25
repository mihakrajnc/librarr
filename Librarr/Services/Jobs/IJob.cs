namespace Librarr.Services.Jobs;

/// <summary>
/// A background job that runs at a set interval
/// </summary>
public interface IJob
{
    /// <summary>
    /// Specifies how long to wait between executions.
    /// </summary>
    TimeSpan Interval { get; }

    /// <summary>
    /// The async method that contains the job's logic.
    /// </summary>
    Task ExecuteAsync(IServiceScope scope, CancellationToken cancellationToken);
}
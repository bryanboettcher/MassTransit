namespace MassTransit.Persistence.Integration.ClaimChecks
{
    /// <summary>
    /// Indicates to the optional hosted service that this implementation can
    /// be used to clean old/stale MessageData values.
    /// </summary>
    public interface IMessageDataCleaner
    {
        /// <summary>
        /// Perform the actual cleanup.  Typically called from a hosted
        /// service to run as a low-priority background task.
        /// </summary>
        /// <returns>The number of entries that were removed</returns>
        Task<int> CleanupAsync(CancellationToken cancellationToken = default);
    }
}

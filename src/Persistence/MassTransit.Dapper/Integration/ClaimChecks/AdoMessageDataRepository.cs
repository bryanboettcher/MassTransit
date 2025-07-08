namespace MassTransit.Dapper.Integration.ClaimChecks
{
    /// <summary>
    /// Implementation of the MessageData middleware, using a SQL table for storage.
    /// </summary>
    public class AdoMessageDataRepository : IMessageDataRepository
    {
        public async Task<Stream> Get(Uri address, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Uri> Put(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

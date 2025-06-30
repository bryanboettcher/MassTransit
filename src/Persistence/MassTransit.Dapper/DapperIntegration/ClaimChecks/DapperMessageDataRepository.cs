namespace MassTransit.DapperIntegration.ClaimChecks
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;


    public class DapperMessageDataRepository : IMessageDataRepository
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

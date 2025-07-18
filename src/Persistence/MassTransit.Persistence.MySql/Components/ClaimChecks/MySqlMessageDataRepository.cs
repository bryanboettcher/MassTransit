namespace MassTransit.MessageData
{
    using System.Data;
    using Persistence.MySql.Connections;

    public class MySqlMessageDataRepository : PessimisticMySqlDatabaseContext<MessageDataSaga>, IMessageDataRepository
    {
        readonly Func<DateTimeOffset> _timeProvider;
        readonly string _removeExpired = "DELETE FROM {0} WHERE Expires < @now;";

        public MySqlMessageDataRepository(
            string connectionString,
            string tableName,
            string idColumnName,
            IsolationLevel isolationLevel,
            Func<DateTimeOffset> timeProvider
        ) : base(connectionString, tableName, idColumnName, isolationLevel)
        {
            _timeProvider = timeProvider;
            _removeExpired = string.Format(_removeExpired, tableName);
        }

        public async Task<Stream> Get(Uri address, CancellationToken cancellationToken = default)
        {
            var id = ClaimChecks.Unpack(address);
            var model = await LoadAsync(id, cancellationToken)
                .ConfigureAwait(false);

            if (model is null)
                throw new KeyNotFoundException($"No claim check available at {address}");

            return model.Data ?? Stream.Null;
        }

        public async Task<Uri> Put(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
        {
            var id = NewId.NextSequentialGuid();
            var now = _timeProvider();
            var later = ClaimChecks.GetExpiration(now, timeToLive);

            var model = new MessageDataSaga
            {
                CorrelationId = id,
                Created = now,
                Expires = later,
                Data = stream
            };

            await InsertAsync(model, cancellationToken)
                .ConfigureAwait(false);

            return ClaimChecks.Pack(id);
        }

        public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
        {
            var now = _timeProvider();

            return await ExecuteAsync(_removeExpired, new { now }, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

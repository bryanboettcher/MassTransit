namespace MassTransit.MessageData
{
    using System.Data;
    using Persistence.SqlServer.Extensions;
    using Microsoft.Data.SqlClient;
    using Persistence.SqlServer.Connections;
    
    public class SqlServerMessageDataRepository : PessimisticSqlServerDatabaseContext<MessageDataSaga>, IMessageDataRepository
    {
        readonly Func<DateTimeOffset> _timeProvider;
        readonly string _removeExpired = "DELETE FROM {0} WHERE Expires < @now;";

        public SqlServerMessageDataRepository(
            string connectionString,
            string tableName,
            string idColumnName,
            IsolationLevel isolationLevel,
            Func<DateTimeOffset> timeProvider
        ): base(connectionString, tableName, idColumnName, isolationLevel)
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

        protected override Func<IDataReader, MessageDataSaga> CreateReaderAdapter() => MapFrom;

        MessageDataSaga MapFrom(IDataReader reader)
        {
            if (reader is not SqlDataReader r)
                throw new InvalidOperationException("Invalid data reader");

            var stream = new MemoryStream();
            r.GetStream("Data").CopyTo(stream, 81920);

            return new MessageDataSaga
            {
                CorrelationId = r.GetGuid(IdColumnName),
                Created = r.GetDateTimeOffset("Created"),
                Expires = r.GetDateTimeOffset("Expires"),
                Data = stream
            };
        }

        protected override Action<object?, SqlParameterCollection> CreateWriterAdapter() => MapTo;

        static void MapTo(object? parameters, SqlParameterCollection collection)
        {
            if (parameters is MessageDataSaga { Data: not null } s)
            {
                using var stream = new MemoryStream();
                s.Data.CopyTo(stream, 81920);

                collection.Add("@correlationid", SqlDbType.Binary).Value = s.CorrelationId.ToByteArray();
                collection.Add("@created", SqlDbType.DateTime).Value = s.Created;
                collection.Add("@expires", SqlDbType.DateTime).Value = s.Expires;
                collection.Add("@data", SqlDbType.VarBinary, -1).Value = stream.ToArray();
                return;
            }

            AssignParameters(parameters, collection);
        }
    }
}

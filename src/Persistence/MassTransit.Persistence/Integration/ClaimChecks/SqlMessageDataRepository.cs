using System.Data;

namespace MassTransit.MessageData
{
    public abstract class SqlMessageDataRepository
    {
        protected const CommandBehavior DefaultBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;

        protected readonly string ConnectionString;
        protected readonly IsolationLevel IsolationLevel;
        protected readonly Func<DateTimeOffset> UtcNowProvider;
        protected readonly CommandBehavior CommandBehavior;
        protected readonly DateTimeOffset MaxExpiration;

        protected IDbConnection? Connection;
        protected IDbTransaction? Transaction;
        protected IDbCommand? Command;

        readonly string _sqlSave;
        readonly string _sqlLoad;
        readonly string _sqlClean;

        protected SqlMessageDataRepository(
            string connectionString,
            string tableName,
            IsolationLevel isolationLevel,
            Func<DateTimeOffset> utcNowProvider,
            CommandBehavior commandBehavior = DefaultBehavior,
            DateTimeOffset? maxExpiration = null
        )
        {
            ConnectionString = connectionString;
            IsolationLevel = isolationLevel;
            UtcNowProvider = utcNowProvider;

            _sqlSave = string.Format(SqlSave, tableName);
            _sqlLoad = string.Format(SqlLoad, tableName);
            _sqlClean = string.Format(SqlClean, tableName);

            CommandBehavior = commandBehavior;
            MaxExpiration = maxExpiration ?? DateTimeOffset.MaxValue;
        }

        protected abstract IDbCommand CreateCommand(string sql, CancellationToken cancellationToken = default);

        /// <inheritdoc cref="IMessageDataRepository.Get"/>
        public async Task<Stream> Get(Uri address, CancellationToken cancellationToken = default)
        {
            var id = Unpack(address);
            var now = UtcNowProvider();

            var command = await CreateCommand(_sqlLoad, cancellationToken)
                .ConfigureAwait(false);

            var output = await Run(
                _sqlLoad,
                GetCallback,
                false,
                cancellationToken
            ).ConfigureAwait(false);

            return output;
        }

        protected abstract Task<Stream> GetCallback<TCommand>(TCommand command, Guid id, DateTimeOffset now, CancellationToken cancellationToken = default)
            where TCommand : IDbCommand;

        /// <inheritdoc cref="IMessageDataRepository.Put"/>
        public async Task<Uri> Put(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
        {
            var id = NewId.NextSequentialGuid();
            var now = UtcNowProvider();
            var expiration = GetExpiration(timeToLive, now);

            if (expiration < now)
                throw new InvalidOperationException("TTL has already expired");

            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);

            await Run(
                _sqlSave,
                PutCallback,
                true,
                cancellationToken
            ).ConfigureAwait(false);

            return Pack(id);
        }
        protected abstract Task PutCallback<TCommand>(TCommand command, Stream stream, Guid id, DateTimeOffset now, DateTimeOffset expiration, CancellationToken cancellationToken = default)
            where TCommand : IDbCommand;

        public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
        {
            var now = UtcNowProvider();
            var rows = 0;

            await Run(
                _sqlClean,
                CleanCallback,
                true,
                cancellationToken
            ).ConfigureAwait(false);

            return rows;
        }

        protected abstract Task<int> CleanCallback<TCommand>(TCommand command, DateTimeOffset now, CancellationToken cancellationToken = default)
            where TCommand : IDbCommand;

        protected abstract string SqlSave { get; }
        protected abstract string SqlLoad {get; }
        protected abstract string SqlClean {get; }

        protected virtual DateTimeOffset GetExpiration(TimeSpan? ttl, DateTimeOffset now)
            => ttl.HasValue ? now.Add(ttl.Value) : MaxExpiration;
        
        protected static Guid Unpack(Uri uri)
        {
            if (uri.Scheme != "urn")
                throw new InvalidOperationException("URI must be a urn");

            if (uri.AbsolutePath != "claim" || string.IsNullOrWhiteSpace(uri.Query) || uri.Query.Length < 2)
                throw new InvalidOperationException("Invalid claim urn format");

            return Guid.Parse(uri.Query.Substring(1));
        }

        protected static Uri Pack(Guid id)
        {
            return new Uri($"urn:claim?{id:N}");
        }
    }
}

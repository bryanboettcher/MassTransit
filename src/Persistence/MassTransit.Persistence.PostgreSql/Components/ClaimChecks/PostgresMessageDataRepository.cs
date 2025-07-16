namespace MassTransit.Persistence.PostgreSql.Components.ClaimChecks
{
    using System.Data;
    using Npgsql;
    using NpgsqlTypes;


    public class PostgresMessageDataRepository : IMessageDataRepository
    {
        const CommandBehavior DefaultBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;

        readonly string _connectionString;
        readonly IsolationLevel _isolationLevel;
        readonly TimeProvider _timeProvider;

        public PostgresMessageDataRepository(string connectionString, string tableName, IsolationLevel isolationLevel, TimeProvider timeProvider)
        {
            _connectionString = connectionString;
            _isolationLevel = isolationLevel;

            _timeProvider = timeProvider;

            SqlLoad = string.Format(SqlLoad, tableName);
            SqlSave = string.Format(SqlSave, tableName);
            SqlClean = string.Format(SqlClean, tableName);
        }

        /// <summary>
        /// The SQL statement used to load a Claim Check from the database.  The {0} value is replaced with the table name.
        /// </summary>
        public string SqlLoad { get; set; } = "SELECT Data FROM {0} WHERE Id = @id AND Expires >= @now LIMIT 1";

        /// <summary>
        /// The SQL statement used to save a Claim Check to the database.  The {0} value is replaced with the table name.
        /// </summary>
        public string SqlSave { get; set; } = "INSERT INTO {0} VALUES (@id, @created, @expires, @data)";

        /// <summary>
        /// The SQL statement used to clean stale Claim Checks.  The {0} value is replaced with the table name.
        /// </summary>
        public string SqlClean { get; set; } = "DELETE FROM {0} WHERE Expires < @now";

        /// <inheritdoc />
        public async Task<Stream> Get(Uri address, CancellationToken cancellationToken = default)
        {
            var id = Unpack(address);
            var now = _timeProvider.GetUtcNow();
            var output = new MemoryStream();

            await CreateCommand(
                SqlLoad,
                async command =>
                {
                    command.Parameters.Add("@id", NpgsqlDbType.Uuid).Value = id;
                    command.Parameters.Add("@now", NpgsqlDbType.TimestampTz).Value = now;

                    await using var reader = await command.ExecuteReaderAsync(DefaultBehavior, cancellationToken)
                        .ConfigureAwait(false);

                    var available = await reader.ReadAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!available)
                        throw new KeyNotFoundException($"No claim check available at {address}");

                    var readerStream = await reader.GetStreamAsync(0, cancellationToken)
                        .ConfigureAwait(false);

                    await readerStream.CopyToAsync(output, cancellationToken)
                        .ConfigureAwait(false);

                    output.Position = 0;
                },
                cancellationToken
            ).ConfigureAwait(false);

            return output;
        }

        /// <inheritdoc />
        public async Task<Uri> Put(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
        {
            var id = NewId.NextSequentialGuid();
            var now = _timeProvider.GetUtcNow();
            var expiration = GetExpiration(timeToLive, now);

            if (expiration < now)
                throw new InvalidOperationException("TTL has already expired");

            await CreateCommand(
                SqlSave,
                async command =>
                {
                    if (stream.CanSeek)
                        stream.Seek(0, SeekOrigin.Begin);

                    command.Parameters.Add("@id", NpgsqlDbType.Uuid).Value = id;
                    command.Parameters.Add("@created", NpgsqlDbType.TimestampTz).Value = now;
                    command.Parameters.Add("@expires", NpgsqlDbType.TimestampTz).Value = expiration;
                    command.Parameters.Add("@data", NpgsqlDbType.Bytea, -1).Value = stream;

                    await command.ExecuteNonQueryAsync(cancellationToken)
                        .ConfigureAwait(false);
                },
                cancellationToken
            ).ConfigureAwait(false);

            return Pack(id);

            static DateTimeOffset GetExpiration(TimeSpan? ttl, DateTimeOffset now)
            {
                return ttl.HasValue
                    ? now.Add(ttl.Value)
                    : DateTimeOffset.MaxValue;
            }
            // C# DTO.MaxValue is the same as Postgres DTO MaxValue
        }

        /// <inheritdoc />
        public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
        {
            var now = _timeProvider.GetUtcNow();
            var rows = 0;

            await CreateCommand(
                SqlClean,
                async command =>
                {
                    command.Parameters.Add("@now", NpgsqlDbType.TimestampTz).Value = now;

                    rows = await command.ExecuteNonQueryAsync(cancellationToken)
                        .ConfigureAwait(false);
                }, cancellationToken
            ).ConfigureAwait(false);

            return rows;
        }

        async Task CreateCommand(string sql, Func<NpgsqlCommand, Task> callback, CancellationToken cancellationToken)
        {
            NpgsqlConnection? connection = null;
            NpgsqlCommand? command = null;
            NpgsqlTransaction? transaction = null;
            try
            {
                connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken)
                    .ConfigureAwait(false);

                transaction = await connection.BeginTransactionAsync(_isolationLevel, cancellationToken)
                    .ConfigureAwait(false);

                command = connection.CreateCommand();

                command.Transaction = transaction;
                command.CommandText = sql;

                await callback(command)
                    .ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                if (command != null)
                    await command.DisposeAsync().ConfigureAwait(false);

                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

                throw;
            }
            finally
            {
                if (transaction is not null)
                    await transaction.DisposeAsync().ConfigureAwait(false);

                if (command is not null)
                    await command.DisposeAsync().ConfigureAwait(false);

                if (connection is not null)
                    await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        static Guid Unpack(Uri uri)
        {
            if (uri.Scheme != "urn")
                throw new InvalidOperationException("URI must be a urn");

            if (uri.AbsolutePath != "claim" || string.IsNullOrWhiteSpace(uri.Query) || uri.Query.Length < 2)
                throw new InvalidOperationException("Invalid claim urn format");

            return Guid.Parse(uri.Query[1..]);
        }

        static Uri Pack(Guid id)
        {
            return new Uri($"urn:claim?{id:N}");
        }
    }
}

namespace MassTransit.Persistence.SqlServer.Components.ClaimChecks
{
    using System.Data;
    using Microsoft.Data.SqlClient;


    public class SqlServerMessageDataRepository : IMessageDataRepository
    {
        const CommandBehavior DefaultBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;

        readonly string _connectionString;
        readonly IsolationLevel _isolationLevel;
        readonly Func<DateTimeOffset> _timeProvider;

        public SqlServerMessageDataRepository(string connectionString, string tableName, IsolationLevel isolationLevel, Func<DateTimeOffset> timeProvider)
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
        public string SqlLoad { get; set; } = "SELECT TOP 1 Data FROM {0} WHERE Id = @id AND Expires >= @now;";

        /// <summary>
        /// The SQL statement used to save a Claim Check to the database.  The {0} value is replaced with the table name.
        /// </summary>
        public string SqlSave { get; set; } = "INSERT INTO {0} VALUES (@id, @created, @expires, @data);";

        /// <summary>
        /// The SQL statement used to clean stale Claim Checks.  The {0} value is replaced with the table name.
        /// </summary>
        public string SqlClean { get; set; } = "DELETE FROM {0} WHERE Expires < @now;";

        /// <inheritdoc />
        public async Task<Stream> Get(Uri address, CancellationToken cancellationToken = default)
        {
            var id = Unpack(address);
            var now = _timeProvider();
            var output = new MemoryStream();

            await CreateCommand(
                SqlLoad,
                async command =>
                {
                    command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = id;
                    command.Parameters.Add("@now", SqlDbType.DateTimeOffset).Value = now;

                    var reader = await command.ExecuteReaderAsync(DefaultBehavior, cancellationToken)
                        .ConfigureAwait(false);

                    var available = await reader.ReadAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!available)
                        throw new KeyNotFoundException($"No claim check available at {address}");

                    await reader.GetStream(0).CopyToAsync(output, 81920, cancellationToken)
                        .ConfigureAwait(false);

                    output.Position = 0;

#if NET8_0_OR_GREATER
                    if (reader is IAsyncDisposable ad)
                        await ad.DisposeAsync().ConfigureAwait(false);
#else
                    reader.Close();
#endif
                },
                cancellationToken
            ).ConfigureAwait(false);

            return output;
        }

        /// <inheritdoc />
        public async Task<Uri> Put(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
        {
            var id = NewId.NextSequentialGuid();
            var now = _timeProvider();
            var expiration = GetExpiration(timeToLive, now);

            if (expiration < now)
                throw new InvalidOperationException("TTL has already expired");

            await CreateCommand(
                SqlSave,
                async command =>
                {
                    if (stream.CanSeek)
                        stream.Seek(0, SeekOrigin.Begin);

                    command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = id;
                    command.Parameters.Add("@created", SqlDbType.DateTimeOffset).Value = now;
                    command.Parameters.Add("@expires", SqlDbType.DateTimeOffset).Value = expiration;
                    command.Parameters.Add("@data", SqlDbType.Binary, -1).Value = stream;

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
            // C# DTO.MaxValue is the same as MSSQL DTO MaxValue
        }

        public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
        {
            var now = _timeProvider();
            var rows = 0;

            await CreateCommand(
                SqlClean,
                async command =>
                {
                    command.Parameters.Add("@now", SqlDbType.DateTimeOffset).Value = now;

                    rows = await command.ExecuteNonQueryAsync(cancellationToken)
                        .ConfigureAwait(false);
                }, cancellationToken
            ).ConfigureAwait(false);

            return rows;
        }

        async Task CreateCommand(string sql, Func<SqlCommand, Task> callback, CancellationToken cancellationToken)
        {
            SqlConnection? connection = null;
            SqlCommand? command = null;
            SqlTransaction? transaction = null;
            try
            {
                connection = new SqlConnection(_connectionString);

                await connection.OpenAsync(cancellationToken)
                    .ConfigureAwait(false);

                transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                    _isolationLevel, cancellationToken).ConfigureAwait(false);

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
                if (command is not null)
                    await command.DisposeAsync().ConfigureAwait(false);

                if (transaction is not null)
                    await transaction.DisposeAsync().ConfigureAwait(false);
                
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

            return Guid.Parse(uri.Query.Substring(1));
        }

        static Uri Pack(Guid id)
        {
            return new Uri($"urn:claim?{id:N}");
        }
    }
}

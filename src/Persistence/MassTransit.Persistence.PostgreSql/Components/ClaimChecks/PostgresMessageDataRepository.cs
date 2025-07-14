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

        /// <summary>
        /// The SQL statement used to load a Claim Check from the database.  The {0} value is replaced with the table name.
        /// </summary>
        public string SqlLoad { get; set; } = "SELECT Data FROM {0} WHERE Id = @id AND Expires >= @now LIMIT 1";

        /// <summary>
        /// The SQL statement used to save a Claim Check to the database.  The {0} value is replaced with the table name.
        /// </summary>
        public string SqlSave { get; set; } = "INSERT INTO {0} VALUES (@id, @created, @expires, @data)";

        public PostgresMessageDataRepository(string connectionString, string tableName, IsolationLevel isolationLevel, TimeProvider timeProvider)
        {
            _connectionString = connectionString;
            _isolationLevel = isolationLevel;
            
            _timeProvider = timeProvider;

            SqlLoad = string.Format(SqlLoad, tableName);
            SqlSave = string.Format(SqlSave, tableName);
        }

        /// <inheritdoc />
        public async Task<Stream> Get(Uri address, CancellationToken cancellationToken = default)
        {
            var id = Unpack(address);
            var now = _timeProvider.GetUtcNow();

            await using var command = await CreateCommand(SqlLoad, cancellationToken)
                .ConfigureAwait(false);

            command.Parameters.Add("@id", NpgsqlDbType.Uuid).Value = id;
            command.Parameters.Add("@now", NpgsqlDbType.TimestampTz).Value = now;

            await using var reader = await command.ExecuteReaderAsync(DefaultBehavior, cancellationToken)
                .ConfigureAwait(false);

            var available = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            
            if (! available)
                throw new KeyNotFoundException($"No claim check available at {address}");

            return await reader.GetStreamAsync(0, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Uri> Put(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
        {
            var id = NewId.NextSequentialGuid();
            var now = _timeProvider.GetUtcNow();
            var expiration = GetExpiration(timeToLive, now);

            if (expiration < now)
                throw new InvalidOperationException("TTL has already expired");

            await using var command = await CreateCommand(SqlSave, cancellationToken);
            
            command.Parameters.Add("@id", NpgsqlDbType.Uuid).Value = id;
            command.Parameters.Add("@created", NpgsqlDbType.TimestampTz).Value = now;
            command.Parameters.Add("@expires", NpgsqlDbType.TimestampTz).Value = expiration;
            command.Parameters.Add("@data", NpgsqlDbType.Bytea, -1).Value = stream;
            
            return Pack(id);

            static DateTimeOffset GetExpiration(TimeSpan? ttl, DateTimeOffset now)
                => ttl.HasValue
                    ? now.Add(ttl.Value)
                    : DateTimeOffset.MaxValue; // C# DTO.MaxValue is the same as MSSQL DTO MaxValue
        }

        async Task<NpgsqlCommand> CreateCommand(string sql, CancellationToken cancellationToken)
        {
            NpgsqlCommand? command = null;
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var transaction = await connection.BeginTransactionAsync(
                    _isolationLevel, cancellationToken).ConfigureAwait(false);

                command = connection.CreateCommand();

                command.Transaction = transaction;
                command.CommandText = sql;

                return command;
            }
            catch
            {
                if (command != null)
                    await command.DisposeAsync();
                throw;
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
            => new($"urn:claim?{id:N}");
    }
}

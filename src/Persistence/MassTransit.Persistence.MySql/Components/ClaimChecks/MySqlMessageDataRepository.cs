namespace MassTransit.MessageData
{
    using System.Data;
    using System.IO;
    using System.Threading;
    using MySqlConnector;

    public class MySqlMessageDataRepository : SqlMessageDataRepository, IMessageDataRepository
    {
        // MySql has a special "max" date that I didn't care to
        // find the actual value for, so now it's just far enough
        // into the future that nobody reading this will care.
        static readonly DateTimeOffset Expiration = new (2199, 12, 31, 23, 59, 59, TimeSpan.Zero);

        public MySqlMessageDataRepository(string connectionString, string tableName, IsolationLevel isolationLevel, Func<DateTimeOffset> timeProvider)
            : base(connectionString, tableName, isolationLevel, timeProvider, maxExpiration: Expiration)
        {
        }

        protected override string SqlLoad => "SELECT Data FROM {0} WHERE Id = @id AND Expires >= @now LIMIT 1;";
        protected override string SqlSave => "INSERT INTO {0} VALUES (@id, @created, @expires, @data);";
        protected override string SqlClean => "DELETE FROM {0} WHERE Expires < @now;";

        protected override async Task<Stream> GetCallback<TCommand>(TCommand command, Guid id, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            if (command is not MySqlCommand cmd)
                throw new InvalidOperationException("MySqlMessageDataRepository only supports MySqlCommand");

            cmd.Parameters.Add("@id", MySqlDbType.Guid).Value = id.ToByteArray();
            cmd.Parameters.Add("@now", MySqlDbType.Timestamp).Value = now;

            await using var reader = await cmd.ExecuteReaderAsync(DefaultBehavior, cancellationToken)
                .ConfigureAwait(false);

            var available = await reader.ReadAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!available)
                throw new KeyNotFoundException($"No claim check available for {id}");

            // I'd like to be able to return the stream directly, but this
            // leads to a lot of nasty issues around disposal and lifetime
            var output = new MemoryStream();
            await reader.GetStream(0)
                .CopyToAsync(output, 81920, cancellationToken)
                .ConfigureAwait(false);

            output.Position = 0;
            return output;
        }

        protected override async Task PutCallback<TCommand>(TCommand command, Stream stream, Guid id, DateTimeOffset now, DateTimeOffset expiration, CancellationToken cancellationToken = default)
        {
            if (command is not MySqlCommand cmd)
                throw new InvalidOperationException("MySqlMessageDataRepository only supports MySqlCommand");
            
            var bufferStream = new MemoryStream();
            await stream.CopyToAsync(bufferStream, 81920, cancellationToken)
                .ConfigureAwait(false);

            var buffer = bufferStream.ToArray();

            cmd.Parameters.Add("@id", MySqlDbType.Guid).Value = id.ToByteArray();
            cmd.Parameters.Add("@created", MySqlDbType.Timestamp).Value = now;
            cmd.Parameters.Add("@expires", MySqlDbType.Timestamp).Value = expiration;
            cmd.Parameters.Add("@data", MySqlDbType.VarBinary, -1).Value = buffer;

            await cmd.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task<int> CleanCallback<TCommand>(TCommand command, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            if (command is not MySqlCommand cmd)
                throw new InvalidOperationException("MySqlMessageDataRepository only supports MySqlCommand");

            cmd.Parameters.Add("@now", MySqlDbType.DateTime).Value = now;

            return await cmd.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        
        async Task Run<TCommand>(string sql, Func<TCommand, Task> callback, bool useTransaction, CancellationToken cancellationToken)
            where TCommand : IDbCommand
        {
            MySqlConnection? connection = null;
            MySqlCommand? command = null;
            MySqlTransaction? transaction = null;
            try
            {
                connection = new MySqlConnection(ConnectionString);

                await connection.OpenAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (useTransaction)
                {
                    transaction = await connection.BeginTransactionAsync(IsolationLevel, cancellationToken)
                        .ConfigureAwait(false);
                }

                command = connection.CreateCommand();

                command.Transaction = transaction;
                command.CommandText = sql;

                await callback(command)
                    .ConfigureAwait(false);

                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
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
    }
}

namespace MassTransit.Dapper.SqlServer.ClaimChecks
{
    using System.Data;
    using Microsoft.Data.SqlClient;

    public class SqlServerMessageDataRepository : IMessageDataRepository
    {
        const CommandBehavior DefaultBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;
        
        readonly string _connectionString;
        readonly string _tableName;
        readonly IsolationLevel _isolationLevel;

        public string SqlLoad { get; set; } = @"SELECT TOP 1 Data FROM {0} WHERE Id = @id AND Expires >= @now;";

        public SqlServerMessageDataRepository(string connectionString, string tableName, IsolationLevel isolationLevel)
        {
            _connectionString = connectionString;
            _tableName = tableName;
            _isolationLevel = isolationLevel;
        }

        public async Task<Stream> Get(Uri address, CancellationToken cancellationToken = default)
        {
            var id = ExtractId(address);
            
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = (SqlTransaction) await connection.BeginTransactionAsync(
                _isolationLevel, cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();

            command.Parameters.AddWithValue("@id", id);

            command.Transaction = transaction;
            command.CommandText = string.Format(SqlLoad, _tableName);

            await using var reader = await command.ExecuteReaderAsync(DefaultBehavior, cancellationToken)
                .ConfigureAwait(false);

            var available = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            
            if (! available)
                throw new KeyNotFoundException($"No claim check available at {address}");

            return reader.GetStream(0);

            static Guid ExtractId(Uri address)
            {
                if (address.Scheme != "urn")
                    throw new ArgumentException("The address must be a urn");
            }
        }

        public async Task<Uri> Put(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
        {
        }
        
        class DbModel
        {
            public Guid Id { get; set; }

        }
    }
}

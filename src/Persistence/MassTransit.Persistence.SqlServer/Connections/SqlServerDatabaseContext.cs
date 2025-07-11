namespace MassTransit.Persistence.SqlServer.Connections
{
    using System.Data;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Integration.Saga;
    using Integration.SqlBuilders;
    using Microsoft.Data.SqlClient;


    public abstract class SqlServerDatabaseContext<TSaga> : SagaDatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        readonly string _connectionString;

        protected readonly string TableName;
        protected readonly string IdColumnName;

        protected SqlConnection? Connection;
        protected SqlTransaction? Transaction;

        bool _disposed;

        protected SqlServerDatabaseContext(string connectionString, string tableName, string idColumnName)
        {
            _connectionString = connectionString;

            TableName = tableName;
            IdColumnName = idColumnName;
        }

        protected static string BuildQueryPredicate(List<SqlPredicate> predicates, Action<string, object?> parameterCallback)
        {
            var queryPredicates = new List<string>();

            foreach (var p in predicates)
            {
                var paramName = $"value{queryPredicates.Count}";
                queryPredicates.Add($"[{p.Name}] {p.Operator} @{paramName}");
                parameterCallback?.Invoke(paramName, p.Value);
            }

            return string.Join(" AND ", queryPredicates);
        }
        
        protected override async IAsyncEnumerable<TSaga> ReadAsync(string sql, object? parameters, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var readerAdapter = CreateReaderAdapter();
            var writerAdapter = CreateWriterAdapter();

            Connection = await CreateConnection(cancellationToken)
                .ConfigureAwait(false);

            await using var command = Connection.CreateCommand();

            if (Transaction is not null)
                command.Transaction = Transaction;

            command.CommandText = sql;

            writerAdapter(parameters, command.Parameters);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken))
            {
                yield return readerAdapter(reader);
            }
        }

        protected override async Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken)
        {
            Connection = await CreateConnection(cancellationToken)
                .ConfigureAwait(false);

            await using var command = Connection.CreateCommand();

            if (Transaction is not null)
                command.Transaction = Transaction;

            command.CommandText = sql;

            if (parameters is not null)
                AssignParameters(parameters, command.Parameters);

            var rows = await command.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);

            return rows;
        }

        protected virtual async Task<SqlConnection> CreateConnection(CancellationToken cancellationToken)
        {
            if (_disposed)
                Debugger.Break();

            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await OnConnectionOpened(connection, cancellationToken);

            return connection;
        }

        protected virtual Func<IDataReader, TSaga> CreateReaderAdapter() => ReflectionsAdapter.CreateFor<TSaga>();

        protected virtual Action<object?, SqlParameterCollection> CreateWriterAdapter()
        {
            return AssignParameters;
        }
        
        protected abstract ValueTask OnConnectionOpened(SqlConnection connection, CancellationToken cancellationToken);

        static void AssignParameters(object? parameters, SqlParameterCollection collection)
        {
            foreach (var (name, value) in ParameterReader.Read(parameters))
            {
                collection.AddWithValue(name, value ?? DBNull.Value);
            }
        }

        public virtual Task CommitAsync(CancellationToken cancellationToken = default)
            => Transaction is null
                ? Task.CompletedTask
                : Transaction.CommitAsync(cancellationToken);

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Transaction?.Dispose();
            Connection?.Dispose();
        }

        public virtual async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (Transaction is not null)
                await Transaction.DisposeAsync().ConfigureAwait(false);

            if (Connection is not null)
                await Connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}

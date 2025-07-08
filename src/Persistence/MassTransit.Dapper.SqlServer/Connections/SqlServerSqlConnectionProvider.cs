namespace MassTransit.Dapper.SqlServer.Connections;

using System.Data;
using Integration.Saga;
using Microsoft.Data.SqlClient;

public class SqlServerSqlConnectionProvider<TModel> : ISagaSqlConnectionProvider<TModel>
    where TModel : class, ISaga
{
    readonly string _connectionString;
    readonly IsolationLevel? _isolationLevel;

    public SqlServerSqlConnectionProvider(string connectionString, IsolationLevel? isolationLevel = null)
    {
        _connectionString = connectionString;
        _isolationLevel = isolationLevel;
    }

    public async Task<ISagaSqlConnection<TModel>> CreateConnection(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var transaction = _isolationLevel is not null
            ? await CreateTransaction(_isolationLevel.Value)
            : null;

        return new SqlServerConnection<TModel>(connection, transaction);

        async Task<SqlTransaction> CreateTransaction(IsolationLevel isolationLevel)
            => (SqlTransaction) await connection.BeginTransactionAsync(
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);
    }
}

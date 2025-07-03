namespace MassTransit.Dapper.SqlServer.Connections;

using System.Data;
using DapperIntegration.Saga;
using Microsoft.Data.SqlClient;
using Saga;

public class SqlServerConnectionProvider<TModel> : ISagaConnectionProvider<TModel>
    where TModel : class, ISaga
{
    readonly string _connectionString;
    readonly IsolationLevel? _isolationLevel;

    public SqlServerConnectionProvider(string connectionString, IsolationLevel? isolationLevel = null)
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
            ? await connection.BeginTransactionAsync(_isolationLevel!.Value, cancellationToken)
                .ConfigureAwait(false) as SqlTransaction
            : null;

        return new SqlServerConnection<TModel>(connection, transaction);
    }
}

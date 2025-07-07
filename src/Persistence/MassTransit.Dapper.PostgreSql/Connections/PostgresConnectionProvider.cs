namespace MassTransit.Dapper.PostgreSql.Connections;

using System.Data;
using MassTransit.DapperIntegration.Saga;
using Npgsql;


public class PostgresConnectionProvider<TModel> : ISagaConnectionProvider<TModel>
    where TModel : class, ISaga
{
    readonly string _connectionString;
    readonly IsolationLevel? _isolationLevel;

    public PostgresConnectionProvider(string connectionString, IsolationLevel? isolationLevel = null)
    {
        _connectionString = connectionString;
        _isolationLevel = isolationLevel;
    }

    public async Task<ISagaSqlConnection<TModel>> CreateConnection(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var transaction = _isolationLevel is not null
            ? await CreateTransaction(_isolationLevel.Value)
            : null;

        return new PostgresConnection<TModel>(connection, transaction);

        async Task<NpgsqlTransaction> CreateTransaction(IsolationLevel isolationLevel)
            => (NpgsqlTransaction) await connection.BeginTransactionAsync(
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);
    }
}

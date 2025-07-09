namespace MassTransit.Dapper.PostgreSql.Connections;

using System.Data;
using Integration.Saga;
using Npgsql;


public class PostgresSagaConnectionProvider<TModel> : ISagaConnectionProvider<TModel>
    where TModel : class, ISaga
{
    readonly string _connectionString;
    readonly IsolationLevel? _isolationLevel;

    public PostgresSagaConnectionProvider(string connectionString, IsolationLevel? isolationLevel = null)
    {
        _connectionString = connectionString;
        _isolationLevel = isolationLevel;
    }

    public async Task<ISagaConnection<TModel>> CreateConnection(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var transaction = _isolationLevel is not null
            ? await CreateTransaction(_isolationLevel.Value)
            : null;

        return new PostgresSagaConnection<TModel>(connection, transaction);

        async Task<NpgsqlTransaction> CreateTransaction(IsolationLevel isolationLevel)
            => (NpgsqlTransaction) await connection.BeginTransactionAsync(
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);
    }
}

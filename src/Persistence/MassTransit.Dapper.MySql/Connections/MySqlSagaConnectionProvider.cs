namespace MassTransit.Dapper.MySqlql.Connections;

using System.Data;
using Integration.Saga;
using MySql.Data.MySqlClient;


public class MySqlSagaConnectionProvider<TModel> : ISagaConnectionProvider<TModel>
    where TModel : class, ISaga
{
    readonly string _connectionString;
    readonly IsolationLevel? _isolationLevel;

    public MySqlSagaConnectionProvider(string connectionString, IsolationLevel? isolationLevel = null)
    {
        _connectionString = connectionString;
        _isolationLevel = isolationLevel;
    }

    public async Task<ISagaConnection<TModel>> CreateConnection(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var transaction = _isolationLevel is not null
            ? await CreateTransaction(_isolationLevel.Value)
            : null;

        return new MySqlSagaConnection<TModel>(connection, transaction);

        async Task<MySqlTransaction> CreateTransaction(IsolationLevel isolationLevel)
            => (MySqlTransaction) await connection.BeginTransactionAsync(
                isolationLevel,
                cancellationToken
            ).ConfigureAwait(false);
    }
}

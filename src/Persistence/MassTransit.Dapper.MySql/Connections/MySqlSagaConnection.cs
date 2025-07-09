namespace MassTransit.Dapper.MySqlql.Connections;

using System.Data;
using System.Runtime.CompilerServices;
using Integration.Saga;
using Integration.SqlBuilders;
using MySql.Data.MySqlClient;


public class MySqlSagaConnection<TModel> : ISagaConnection<TModel>
    where TModel : class, ISaga
{
    readonly MySqlConnection _connection;
    readonly MySqlTransaction? _transaction;

    bool _disposed;

    public MySqlSagaConnection(MySqlConnection connection, MySqlTransaction? transaction)
    {
        _connection = connection;
        _transaction = transaction;
        _disposed = false;
    }

    public async IAsyncEnumerable<TModel> ReadAsync(
        string query,
        object? parameters = null,
        Func<IDataReader, TModel>? adapter = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        adapter ??= ReflectionsAdapter.CreateFor<TModel>();

        await using var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = query;

        if (parameters is not null)
            AssignParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return adapter(reader);
        }
    }

    public async Task<int> RunAsync(
        string query,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = query;

        if (parameters is not null)
            AssignParameters(command, parameters);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return _transaction?.CommitAsync(cancellationToken)
            ?? Task.CompletedTask;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_transaction is not null)
            await _transaction.DisposeAsync().ConfigureAwait(false);

        await _connection.DisposeAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _transaction?.Dispose();
        _connection.Dispose();

        GC.SuppressFinalize(this);
        _disposed = true;
    }

    static void AssignParameters(MySqlCommand command, object? parameters)
    {
        foreach (var (name, value) in ParameterReader.Read(parameters))
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }
}

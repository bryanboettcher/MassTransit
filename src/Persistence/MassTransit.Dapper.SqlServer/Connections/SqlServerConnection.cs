using MassTransit.DapperIntegration.SqlBuilders;

namespace MassTransit.Dapper.SqlServer.Connections;

using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using DapperIntegration.Saga;
using Microsoft.Data.SqlClient;

public class SqlServerConnection<TModel> : ISagaSqlConnection<TModel>
    where TModel : class, ISaga
{
    readonly SqlConnection _connection;
    readonly SqlTransaction? _transaction;

    public SqlServerConnection(SqlConnection connection, SqlTransaction? transaction)
    {
        _connection = connection;
        _transaction = transaction;
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

    static void AssignParameters(SqlCommand command, object? parameters)
    {
        foreach (var (name, value) in ParameterReader.Read(parameters))
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync().ConfigureAwait(false);

        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection.Dispose();
    }
}

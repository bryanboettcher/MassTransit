namespace MassTransit.DapperIntegration.Saga;

using System.Data;

public interface ISagaConnectionProvider<TSaga>
    where TSaga : class
{
    Task<ISagaSqlConnection<TSaga>> CreateConnection(CancellationToken cancellationToken = default);
}

/// <summary>
/// Stateful object that represents the lifecycle of a single connection, query, transaction, etc.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public interface ISagaSqlConnection<TSaga> : IAsyncDisposable, IDisposable
    where TSaga : class
{
    IAsyncEnumerable<TSaga> ReadAsync(
        string query,
        object? parameters = null,
        Func<IDataReader, TSaga>? adapter = null,
        CancellationToken cancellationToken = default
    );

    Task<int> RunAsync(
        string query,
        object? parameters = null,
        CancellationToken cancellationToken = default
    );
}

namespace MassTransit.Dapper.Integration.Saga;

using System.Data;
using System.Data.Common;


/// <summary>
/// Stateful object that represents the lifecycle of a single connection, query, transaction, etc.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public interface ISagaConnection<TSaga> : IAsyncDisposable, IDisposable
    where TSaga : class
{
    /// <summary>
    /// Implements a forward-only reader to provide zero-to-infinite rows from a result set.
    /// </summary>
    /// <param name="query">The SQL to execute</param>
    /// <param name="parameters">Any parameters needed for the SQL</param>
    /// <param name="adapter">A converter to rehydrate an instance of <typeparamref name="TSaga"/>.  Defaults to a reflections-based converter.</param>
    /// <param name="parameterCallback"></param>
    /// <param name="cancellationToken">A cancellationToken to abort the operation</param>
    IAsyncEnumerable<TSaga> ReadAsync(
        string query,
        object? parameters = null,
        Func<IDataReader, TSaga>? adapter = null,
        Action<DbParameterCollection>? parameterCallback = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Builds and runs a command against the database without returning anything but
    /// the number of affected rows.
    /// </summary>
    /// <param name="query">The SQL to execute</param>
    /// <param name="parameters">Any parameters needed for the SQL</param>
    /// <param name="parameterCallback"></param>
    /// <param name="cancellationToken">A cancellationToken to abort the operation</param>
    Task<int> RunAsync(
        string query,
        object? parameters = null,
        Action<DbParameterCollection>? parameterCallback = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Commits any underlying transactions that may have been started.
    /// </summary>
    /// <param name="cancellationToken">A cancellationToken to abort the operation</param>
    Task CommitAsync(CancellationToken cancellationToken);
}

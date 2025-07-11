namespace MassTransit.Dapper.Integration.Saga;

/// <summary>
/// A generics-constrained factory to create instances of saga database connections.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public interface ISagaConnectionProvider<TSaga>
    where TSaga : class
{
    /// <summary>
    /// Creates a ready-to-use instance of <seealso cref="ISagaConnection{TSaga}"/>.
    /// </summary>
    /// <param name="cancellationToken">A cancellationToken to abort the operation</param>
    /// <returns></returns>
    Task<ISagaConnection<TSaga>> CreateConnection(CancellationToken cancellationToken = default);
}

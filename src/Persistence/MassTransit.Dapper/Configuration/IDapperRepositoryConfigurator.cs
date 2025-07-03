namespace MassTransit.Dapper.Configuration
{
    using System.Data;
    using DapperIntegration.Saga;
    using DapperIntegration.SqlBuilders;

    /// <summary>
    /// Enables an ADO.NET-based saga repository for neurotic-levels of control over the process.
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    public interface IDapperRepositoryConfigurator<TSaga>
        where TSaga : class
    {
        /// <summary>
        /// Allows for full control over creating the saga repository.  Only use this if the
        /// other configuration methods are insufficiently flexible to configure the repository.
        /// </summary>
        IDapperRepositoryConfigurator<TSaga> SetContextFactory(DatabaseContextFactory<TSaga> contextFactory);

        /// <summary>
        /// Allows specifying a custom SQL generator for this saga.  A custom formatter must be
        /// specified if not using one of the additional libraries providing them.
        /// </summary>
        IDapperRepositoryConfigurator<TSaga> SetSqlFormatter(ISagaSqlFormatter<TSaga> formatter);

        /// <summary>
        /// Allows specifying a custom connection provider.  Connection providers are generally
        /// thin wrappers over an underlying database connection, abstracting connection-specific
        /// behaviors away.  The connection provider must be specified if not using one of the
        /// additional libraries providing one.
        /// </summary>
        IDapperRepositoryConfigurator<TSaga> SetConnectionProvider(ISagaConnectionProvider<TSaga> connectionProvider);
    }
}

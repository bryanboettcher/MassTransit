namespace MassTransit.Configuration;

using System.Data;
using DapperIntegration.Saga;


public interface IDapperJobSagaRepositoryConfigurator
{
    /// <summary>
    /// Override the context factory for the <see cref="JobSaga"/> repository.
    /// </summary>
    void UseJobContextFactory(Func<IServiceProvider, DatabaseContextFactory<JobSaga>> factoryFunc);

    /// <summary>
    /// Override the context factory for the <see cref="JobTypeSaga"/> repository.
    /// </summary>
    void UseJobTypeContextFactory(Func<IServiceProvider, DatabaseContextFactory<JobTypeSaga>> factoryFunc);

    /// <summary>
    /// Override the context factory for the <see cref="JobAttemptSaga"/> repository.
    /// </summary>
    void UseJobAttemptContextFactory(Func<IServiceProvider, DatabaseContextFactory<JobAttemptSaga>> factoryFunc);

    /// <summary>
    /// Configures the saga to use Microsoft SQL Server with this connection string.
    /// Can also be set with <seealso cref="DapperOptions{TSaga}.ConnectionString"/> and <seealso cref="DapperOptions{TSaga}.Provider"/>.
    /// </summary>
    /// <param name="connectionString">The connection string to use</param>
    void UseSqlServer(string connectionString);

    /// <summary>
    /// Configures the saga to use PostgreSQL with this connection string.
    /// Can also be set with <seealso cref="DapperOptions{TSaga}.ConnectionString"/> and <seealso cref="DapperOptions{TSaga}.Provider"/>.
    /// </summary>
    /// <param name="connectionString">The connection string to use</param>
    void UsePostgres(string connectionString);

    /// <summary>
    /// Configures the saga to use this transaction level.  Defaults to <see cref="System.Data.IsolationLevel.Serializable"/>.
    /// Can also be set with <seealso cref="DapperOptions{TSaga}.IsolationLevel"/>.
    /// </summary>
    /// <param name="isolationLevel">The isolation level to use for all operations for this saga</param>
    void UseIsolationLevel(IsolationLevel isolationLevel);
}
namespace MassTransit;

using DapperIntegration.Saga;
using DapperIntegration.SqlBuilders;


public interface IDapperRepositoryConfigurator<TSaga> :
    IDapperSagaRepositoryConfigurator
    where TSaga : class, ISaga
{
    /// <summary>
    /// Use a custom SQL formatter to build INSERT/UPDATE/DELETE/SELECT statements for this saga.  If set,
    /// this formatter will be used for the default context factory, implemented by <see cref="SagaDatabaseContext{TSaga}"/>.
    /// It is not necessary if a fully custom context factory is provided via <see cref="SetContextFactory"/>.
    /// </summary>
    /// <param name="factory">The factory for the formatter</param>
    void SetSqlFormatter(Func<IServiceProvider, ISagaSqlFormatter<TSaga>> factory);

    /// <summary>
    /// Use a custom repository for this saga.  Saga repositories are responsible for the actual persistence
    /// of the code models and the storage provider.  A custom repository allows for fully custom behavior at
    /// every step of the saving/loading process.
    /// </summary>
    /// <param name="factory">The factory for the database context</param>
    void SetContextFactory(Func<IServiceProvider, DatabaseContextFactory<TSaga>> factory);
}

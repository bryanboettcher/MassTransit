#nullable enable
namespace MassTransit
{
    using System;
    using System.Data;
    using DapperIntegration.SqlBuilders;

    public interface IDapperJobSagaRepositoryConfigurator
    {
        void UseJobContextFactory(Func<IServiceProvider, DatabaseContextFactory<JobSaga>> factoryFunc);
        void UseJobTypeContextFactory(Func<IServiceProvider, DatabaseContextFactory<JobTypeSaga>> factoryFunc);
        void UseJobAttemptContextFactory(Func<IServiceProvider, DatabaseContextFactory<JobAttemptSaga>> factoryFunc);
        void UseSqlServer(string connectionString);
        void UsePostgres(string connectionString);
        void UseIsolationLevel(IsolationLevel isolationLevel);
    }

    public interface IDapperSagaRepositoryConfigurator
    {
        void UseSqlServer(string connectionString);
        void UsePostgres(string connectionString);
        void UseIsolationLevel(IsolationLevel isolationLevel);
        void UseTableName(string tableName);
        void UseIdColumnName(string idColumnName);
    }

    public interface IDapperSagaRepositoryConfigurator<TSaga> :
        IDapperSagaRepositoryConfigurator
        where TSaga : class, ISaga
    {
        /// <summary>
        /// Set the database context factory to allow customization of the Dapper interaction/queries
        /// </summary>
        [Obsolete("Use UseContextFactory() instead", true)]
        DatabaseContextFactory<TSaga> ContextFactory { get; set; }

        void UseSqlBuilder(Func<IServiceProvider, ISagaSqlFormatter<TSaga>> factory);
        void UseContextFactory(Func<IServiceProvider, DatabaseContextFactory<TSaga>> factory);
    }
    #nullable restore
}

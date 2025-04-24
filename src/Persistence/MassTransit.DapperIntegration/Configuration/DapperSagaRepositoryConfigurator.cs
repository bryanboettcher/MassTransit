#nullable enable

namespace MassTransit.Configuration
{
    using System.Collections.Generic;
    using System.Data;
    using DapperIntegration.Saga;
    using DapperIntegration.SqlBuilders;
    using Saga;
    using Microsoft.Extensions.DependencyInjection;
    
    public class DapperSagaRepositoryConfigurator<TSaga> :
        IDapperSagaRepositoryConfigurator<TSaga>,
        ISpecification
        where TSaga : class, ISaga
    {
        readonly string _connectionString;

        public DapperSagaRepositoryConfigurator(string connectionString = default, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            _connectionString = connectionString;
            IsolationLevel = isolationLevel;
        }

        public string ConnectionString { get; set; }
        public string TableName { get; set; }
        public string IdColumnName { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public DapperDatabaseProvider Provider { get; set; }
        public SqlBuilder<TSaga>? SqlBuilder { get; set; }
        public DatabaseContextFactory<TSaga>? ContextFactory { get; set; }
        
        public IDapperSagaRepositoryConfigurator UseSqlServer(string connectionString)
        {
            ConnectionString = connectionString;
            Provider = DapperDatabaseProvider.SqlServer;

            return this;
        }

        public IDapperSagaRepositoryConfigurator UsePostgres(string connectionString)
        {
            ConnectionString = connectionString;
            Provider = DapperDatabaseProvider.Postgres;

            return this;
        }

        public IDapperSagaRepositoryConfigurator UseIsolationLevel(IsolationLevel isolationLevel)
        {
            IsolationLevel = isolationLevel;
            return this;
        }

        public IDapperSagaRepositoryConfigurator UseTableName(string tableName)
        {
            TableName = tableName;
            return this;
        }

        public IDapperSagaRepositoryConfigurator UseIdColumnName(string idColumnName)
        {
            IdColumnName = idColumnName;
            return this;
        }

        public IDapperSagaRepositoryConfigurator UseProvider(DapperDatabaseProvider provider)
        {
            Provider = provider;
            return this;
        }

        public IDapperSagaRepositoryConfigurator<TSaga> UseSqlBuilder(SqlBuilder<TSaga> sqlBuilder)
        {
            SqlBuilder = sqlBuilder;
            return this;
        }

        public IDapperSagaRepositoryConfigurator<TSaga> UseContextFactory(DatabaseContextFactory<TSaga> contextFactory)
        {
            ContextFactory = contextFactory;
            return this;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                yield return this.Failure("ConnectionString", "must be specified");
        }

        public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
        {
            // TODO: more happens here
            configurator.AddOptions<DapperOptions<TSaga>>();

            configurator.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>,
                DapperSagaRepositoryContextFactory<TSaga>>();
        }
    }
}

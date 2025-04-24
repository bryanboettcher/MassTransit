#nullable enable

namespace MassTransit.Configuration
{
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using DapperIntegration.Saga;
    using DapperIntegration.SqlBuilders;
    using Saga;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;


    public class DapperSagaRepositoryConfigurator<TSaga> :
        IDapperSagaRepositoryConfigurator<TSaga>,
        ISpecification
        where TSaga : class, ISaga
    {
        public DapperSagaRepositoryConfigurator(string? connectionString = null, IsolationLevel? isolationLevel = null)
        {
            if (connectionString is not null)
                ConnectionString = connectionString;

            if (isolationLevel is not null)
                IsolationLevel = isolationLevel;
        }

        public string? ConnectionString { get; set; }
        public string? TableName { get; set; }
        public string? IdColumnName { get; set; }
        public IsolationLevel? IsolationLevel { get; set; }
        public DapperDatabaseProvider? Provider { get; set; }
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
            // TODO: handle validation better
            //
            // It is entirely possible that the entirety of the configuration could
            // happen inside a `services.AddOptions<>()` call in the caller's code.
            // In this case, we (by design) won't have properties here, unless we can
            // resolve the DapperOptions when building the IDapperSagaRepositoryConfigurator.
            // If we can do that, then all the properties could be set ahead of time
            // from the DapperOptions object, and the validation will work fine.

            //if (string.IsNullOrWhiteSpace(_connectionString))
            //    yield return this.Failure("ConnectionString", "must be specified");

            yield break;
        }

        public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
        {
            if (configurator.All(r => r.ServiceType != typeof(IOptions<DapperOptions<TSaga>>)))
            {
                configurator.AddOptions<DapperOptions<TSaga>>().Configure(opt =>
                {
                    opt.ConnectionString = ConnectionString;
                    opt.IsolationLevel = IsolationLevel;
                    opt.Provider = Provider;
                    opt.IdColumnName = IdColumnName;
                    opt.TableName = TableName;
                    opt.SqlBuilder = SqlBuilder;
                    opt.ContextFactory = ContextFactory;
                });
            }

            configurator.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>,
                DapperSagaRepositoryContextFactory<TSaga>>();
        }
    }
}

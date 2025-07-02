namespace MassTransit.Dapper.Configuration
{
    using System.Data;
    using DapperIntegration.Saga;
    using DapperIntegration.SqlBuilders;
    using MassTransit.Configuration;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Saga;


    public interface IDapperRepositoryConfigurator<TSaga>
        where TSaga : class
    {
        /// <summary>
        /// Allows for full control over creating the saga repository.  Only use this if the
        /// other configuration methods are insufficiently flexible to configure the repository.
        /// </summary>
        IDapperRepositoryConfigurator<TSaga> SetContextFactory(DatabaseContextFactory<TSaga> contextFactory);
        IDapperRepositoryConfigurator<TSaga> SetSqlFormatter(ISagaSqlFormatter<TSaga> formatter);
        IDapperRepositoryConfigurator<TSaga> SetConnectionProvider(ISagaConnectionProvider<TSaga> connectionProvider);
    }

    public class DapperRepositoryConfigurator<TSaga> : IDapperRepositoryConfigurator<TSaga>, ISpecification
        where TSaga : class, ISaga
    {
        DatabaseContextFactory<TSaga>? _contextFactory;
        ISagaSqlFormatter<TSaga>? _formatter;
        ISagaConnectionProvider<TSaga>? _connectionProvider;

        public IDapperRepositoryConfigurator<TSaga> SetContextFactory(DatabaseContextFactory<TSaga> contextFactory)
        {
            _contextFactory = contextFactory;
            return this;
        }

        public IDapperRepositoryConfigurator<TSaga> SetSqlFormatter(ISagaSqlFormatter<TSaga> formatter)
        {
            _formatter = formatter;
            return this;
        }

        public IDapperRepositoryConfigurator<TSaga> SetConnectionProvider(ISagaConnectionProvider<TSaga> connectionProvider)
        {
            _connectionProvider = connectionProvider;
            return this;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (_contextFactory is null)
                yield return this.Failure("ContextFactory must be set");

            if (_formatter is null)
                yield return this.Failure("SqlFormatter must be set");

            if (_connectionProvider is null)
                yield return this.Failure("ConnectionProvider must be set");
        }

        public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> services)
        {
            services.TryAddScoped(_ => _formatter!);
            services.TryAddScoped(_ => _connectionProvider!);

            services.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            services.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            services.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>, DapperSagaRepositoryContextFactory<TSaga>>();
        }
    }

    public static class SagaRegistrationConfiguratorExtensions
    {
        public static void DapperRepository<TSaga>(
            this ISagaRegistrationConfigurator<TSaga> sagaRegistration,
            Action<IDapperRepositoryConfigurator<TSaga>> configure
        ) where TSaga : class, ISaga
        {
            var configuration = new DapperRepositoryConfigurator<TSaga>();

            configure.Invoke(configuration);

            configuration.Validate().ThrowIfContainsFailure("The saga repository configuration is invalid:");
            sagaRegistration.Repository(configuration.Register);
        }
    }

    public delegate Task<DatabaseContext<TSaga>> DatabaseContextFactory<TSaga>(IServiceProvider serviceProvider)
        where TSaga : class;
}

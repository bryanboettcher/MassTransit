#nullable enable
namespace MassTransit.DapperIntegration.Configuration
{
    using System.Collections.Generic;
    using System.Data;
    using MassTransit.Configuration;
    using MassTransit.Saga;
    using Microsoft.Extensions.DependencyInjection;
    using Saga;


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

        public string ConnectionString { get; private set; }

        public IsolationLevel IsolationLevel { get; set; }

        public DatabaseContextFactory<TSaga>? ContextFactory { get; set; }

        public IEnumerable<ValidationResult> Validate()
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                yield return this.Failure("ConnectionString", "must be specified");
        }

        public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
        {
            configurator.AddOptions<DapperOptions<TSaga>>()
                .Configure(opt =>
                {
                    opt.UseSqlServer(_connectionString);
                    opt.UseIsolationLevel(IsolationLevel);
                });

            configurator.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>,
                DapperSagaRepositoryContextFactory<TSaga>>();
        }
    }
}

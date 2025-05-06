using MassTransit.Internals;

namespace MassTransit.Configuration
{
    using System;
    
    public class DapperSagaRepositoryRegistrationProvider :
        ISagaRepositoryRegistrationProvider
    {
        readonly Action<IDapperSagaRepositoryConfigurator> _configure;
        readonly string _connectionString;

        public DapperSagaRepositoryRegistrationProvider(Action<IDapperSagaRepositoryConfigurator> configure)
        {
            _connectionString = string.Empty;
            _configure = configure;
        }

        [Obsolete("Set connection string via the configurator callback or IOptions<DapperOptions<TSaga>>", false)]
        public DapperSagaRepositoryRegistrationProvider(string connectionString, Action<IDapperSagaRepositoryConfigurator> configure)
        {
            _connectionString = connectionString;
            _configure = configure;
        }

        public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
            where TSaga : class, ISaga
        {
            configurator.DapperRepository(_connectionString, r => _configure?.Invoke(r));
        }
    }

    public class DapperJobSagaRepositoryRegistrationProvider :
        ISagaRepositoryRegistrationProvider
    {
        readonly Action<IDapperJobSagaRepositoryConfigurator> _configure;

        public DapperJobSagaRepositoryRegistrationProvider(Action<IDapperJobSagaRepositoryConfigurator> configure)
        {
            _configure = configure;
        }

        public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
            where TSaga : class, ISaga
        {
            var jobConfigurator = new DapperJobSagaRepositoryConfigurator();
            _configure?.Invoke(jobConfigurator);

            switch (configurator)
            {
                case ISagaRegistrationConfigurator<JobSaga> jobConfig:
                    jobConfig.Repository(jobConfigurator.RegisterJob);
                    break;

                case ISagaRegistrationConfigurator<JobTypeSaga> jobTypeConfig:
                    jobTypeConfig.Repository(jobConfigurator.RegisterJobType);
                    break;

                case ISagaRegistrationConfigurator<JobAttemptSaga> jobAttemptConfig:
                    jobAttemptConfig.Repository(jobConfigurator.RegisterJobAttempt);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected configurator type: {configurator.GetType().GetTypeName()}");
            }
        }
    }
}

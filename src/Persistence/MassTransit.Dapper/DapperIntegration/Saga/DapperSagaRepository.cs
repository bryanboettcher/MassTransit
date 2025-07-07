namespace MassTransit.Dapper.DapperIntegration.Saga
{
    using Configuration;
    using DependencyInjection.Registration;
    using MassTransit.DapperIntegration.Saga;
    using MassTransit.Saga;
    using Microsoft.Extensions.DependencyInjection;


    public class DapperSagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        private DapperSagaRepository() { }

        public static ISagaRepository<TSaga> Create(Action<IDapperRepositoryConfigurator<TSaga>> configure, IServiceProvider? provider = null)
        {
            var configurator = new DapperRepositoryConfigurator<TSaga>();

            configure(configurator);

            if (provider is null)
            {
                IServiceCollection services = new ServiceCollection();
                var registrationConfigurator = new SagaRepositoryRegistrationConfigurator<TSaga>(services);

                configurator.Register(registrationConfigurator);
                provider = services.BuildServiceProvider();
            }

            var consumeContextFactory = new SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>();

            var repositoryContextFactory = new DapperSagaRepositoryContextFactory<TSaga>(consumeContextFactory, provider);

            return new SagaRepository<TSaga>(
                repositoryContextFactory,
                repositoryContextFactory,
                repositoryContextFactory
            );
        }
    }
}

namespace MassTransit.Dapper.Integration.Saga
{
    using Configuration;
    using DependencyInjection.Registration;
    using MassTransit.Saga;
    using Microsoft.Extensions.DependencyInjection;

    public class AdoSagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        private AdoSagaRepository() { }

        public static ISagaRepository<TSaga> Create(Action<IAdoRepositoryConfigurator<TSaga>> configure, IServiceProvider? provider = null)
        {
            var configurator = new AdoRepositoryConfigurator<TSaga>();

            configure(configurator);
            configurator.Validate().ThrowIfContainsFailure("Saga repository configuration is invalid:");

            if (provider is null)
            {
                IServiceCollection services = new ServiceCollection();
                var registrationConfigurator = new SagaRepositoryRegistrationConfigurator<TSaga>(services);

                configurator.Register(registrationConfigurator);
                provider = services.BuildServiceProvider();
            }

            var consumeContextFactory = new SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>();

            var repositoryContextFactory = new AdoSagaRepositoryContextFactory<TSaga>(consumeContextFactory, provider);

            return new SagaRepository<TSaga>(
                repositoryContextFactory,
                repositoryContextFactory,
                repositoryContextFactory
            );
        }
    }
}

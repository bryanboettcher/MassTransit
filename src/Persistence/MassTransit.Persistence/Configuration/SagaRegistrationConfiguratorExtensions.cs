namespace MassTransit.Persistence.Configuration;

public static class SagaRegistrationConfiguratorExtensions
{
    /// <summary>
    /// Adds middleware to use custom repositories for sagas.
    /// </summary>
    public static void DapperRepository<TSaga>(
        this ISagaRegistrationConfigurator<TSaga> sagaRegistration,
        Action<IAdoRepositoryConfigurator<TSaga>> configure
    ) where TSaga : class, ISaga
    {
        var configurator = new AdoRepositoryConfigurator<TSaga>();

        configure.Invoke(configurator);

        configurator.Validate().ThrowIfContainsFailure("The saga repository configuration is invalid:");
        sagaRegistration.Repository(configurator.Register);
    }

    /// <summary>
    /// Adds middleware to use custom repositories for Job Consumers.
    /// </summary>
    public static void DapperRepository(this IJobSagaRegistrationConfigurator jobSagaRegistration,
        Action<IAdoJobSagaRepositoryConfigurator> configure)
    {
        var configurator = new AdoJobSagaRepositoryConfigurator();

        configure.Invoke(configurator);

        configurator.Validate().ThrowIfContainsFailure("The job saga repository configuration is invalid:");
        jobSagaRegistration.UseRepositoryRegistrationProvider(configurator);
    }
}

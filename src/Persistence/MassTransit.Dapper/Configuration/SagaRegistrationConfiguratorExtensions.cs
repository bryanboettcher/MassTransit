namespace MassTransit.Dapper.Configuration;

public static class SagaRegistrationConfiguratorExtensions
{
    public static void DapperRepository<TSaga>(
        this ISagaRegistrationConfigurator<TSaga> sagaRegistration,
        Action<IDapperRepositoryConfigurator<TSaga>> configure
    ) where TSaga : class, ISaga
    {
        var configurator = new DapperRepositoryConfigurator<TSaga>();

        configure.Invoke(configurator);

        configurator.Validate().ThrowIfContainsFailure("The saga repository configuration is invalid:");
        sagaRegistration.Repository(configurator.Register);
    }

    public static void DapperRepository(this IJobSagaRegistrationConfigurator jobSagaRegistration,
        Action<IDapperJobSagaRepositoryConfigurator> configure)
    {
        var configurator = new DapperJobSagaRepositoryConfigurator();

        configure.Invoke(configurator);

        configurator.Validate().ThrowIfContainsFailure("The job saga repository configuration is invalid:");
        jobSagaRegistration.UseRepositoryRegistrationProvider(configurator);
    }
}

namespace MassTransit.Dapper.Configuration;

using MassTransit.Configuration;


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

public interface IDapperJobSagaRepositoryConfigurator {}
public class DapperJobSagaRepositoryConfigurator : IDapperJobSagaRepositoryConfigurator, ISpecification,
    ISagaRepositoryRegistrationProvider
{
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
    }
}

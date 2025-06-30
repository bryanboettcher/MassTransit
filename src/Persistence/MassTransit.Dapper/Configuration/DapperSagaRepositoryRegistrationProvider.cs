namespace MassTransit;

using Configuration;


public class DapperSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    readonly Action<IDapperSagaRepositoryConfigurator> _configure;

    public DapperSagaRepositoryRegistrationProvider(Action<IDapperSagaRepositoryConfigurator> configure)
    {
        _configure = configure;
    }

    public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.DapperRepository(r => _configure?.Invoke(r));
    }
}

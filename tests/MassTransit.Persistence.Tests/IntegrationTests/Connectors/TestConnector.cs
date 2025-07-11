namespace MassTransit.Persistence.Tests.IntegrationTests.Connectors;

using Configuration;


public interface TestConnector
{
    Task Setup();
    Task Reset();
    Task Teardown();

    void Connect(IAdoJobSagaRepositoryConfigurator conf);
    void Connect<TSaga>(IAdoRepositoryConfigurator<TSaga> conf)
        where TSaga : class, ISaga;

    Task<List<TSaga>> GetSagas<TSaga>()
        where TSaga : class, ISaga;
}

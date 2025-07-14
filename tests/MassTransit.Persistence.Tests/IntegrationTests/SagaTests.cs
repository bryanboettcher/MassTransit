namespace MassTransit.Persistence.Tests.IntegrationTests
{
    using Connectors;
    using MassTransit.TestFramework;
    using NUnit.Framework;


    public abstract class SagaTests<TConnector> : InMemoryTestFixture
        where TConnector: TestConnector, new()
    {
        protected readonly TConnector Connector;

        protected readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
        
        protected static readonly Guid SagaId = Guid.Parse("d747db39-0d64-49b5-85f4-2a796ba82130");

        protected SagaTests()
        {
            Connector = new TConnector();
        }

        [SetUp]
        public Task Initialize() => Connector.Setup();

        [TearDown]
        public Task Teardown() => Connector.Teardown();
        
        protected Task<List<TSaga>> GetSagas<TSaga>()
            where TSaga : class, ISaga =>
            Connector.GetSagas<TSaga>();
    }
}

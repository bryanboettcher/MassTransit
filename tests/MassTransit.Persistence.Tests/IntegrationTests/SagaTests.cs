namespace MassTransit.Persistence.Tests.IntegrationTests
{
    using Connectors;
    using MassTransit.TestFramework;
    using NUnit.Framework;


    public abstract class SagaTests<TConnector> : InMemoryTestFixture
        where TConnector: TestConnector, new()
    {
        protected readonly TConnector Connector;

        protected readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);
        
        protected static readonly Guid SagaId = Guid.Parse("d747db39-0d64-49b5-85f4-2a796ba82130");

        protected SagaTests()
        {
            Connector = new TConnector();
        }

        [OneTimeSetUp]
        public Task Initialize() => Connector.Setup();

        [OneTimeTearDown]
        public Task Teardown() => Connector.Teardown();

        [SetUp]
        public Task Setup() => Connector.Reset();

        protected Task<List<TSaga>> GetSagas<TSaga>()
            where TSaga : class, ISaga =>
            Connector.GetSagas<TSaga>();
    }
}

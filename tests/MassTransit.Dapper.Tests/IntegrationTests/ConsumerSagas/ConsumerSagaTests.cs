namespace MassTransit.Dapper.Tests.IntegrationTests.ConsumerSagas
{
    using System.Threading.Tasks;
    using MassTransit.Dapper.Integration.Saga;
    using MassTransit.Dapper.SqlServer.Configuration;
    using MassTransit.Dapper.Tests.Common;
    using MassTransit.Testing;
    using NUnit.Framework;


    [Category("Integration")]
    [TestFixture]
    public class ConsumerSagaTests : SagaTests
    {
        ISagaRepository<VersionedConsumerSaga> _repository;
        IServiceProvider _provider;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _repository = AdoSagaRepository<VersionedConsumerSaga>.Create(conf =>
                conf.UsingSqlServer(
                    ConnectionString, sql => sql.SetTableName("VersionedSagas").SetOptimisticConcurrency()
                )
            );

            configurator.Saga(_repository);
            base.ConfigureInMemoryReceiveEndpoint(configurator);
        }

        [Test]
        public async Task CreateMessage_creates_saga()
        {
            await InputQueueSendEndpoint.Send<CreateSaga>(new { CorrelationId = SagaId, Name = "my saga" });
            await BusTestHarness.Consumed.Any<CreateSaga>();

            var found = await _repository.ShouldContainSaga(SagaId, DefaultTimeout);
            Assert.That(found, Is.EqualTo(SagaId));

            var sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Not.Empty);
            Assert.That(sagas.Count, Is.EqualTo(1));
            Assert.That(sagas[0].Name, Is.EqualTo("my saga"));
        }
        
        [Test]
        public async Task UpdateMessage_updates_saga()
        {
            await InputQueueSendEndpoint.Send<CreateSaga>(new { CorrelationId = SagaId, Name = "my saga 0" });
            await BusTestHarness.Consumed.Any<CreateSaga>();

            await InputQueueSendEndpoint.Send<UpdateSaga>(new { CorrelationId = SagaId, Name = "my saga 1" });
            await BusTestHarness.Consumed.Any<UpdateSaga>();

            var found = await _repository.ShouldContainSaga(SagaId, DefaultTimeout);
            Assert.That(found, Is.EqualTo(SagaId));

            var sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Not.Empty);
            Assert.That(sagas.Count, Is.EqualTo(1));
            Assert.That(sagas[0].Name, Is.EqualTo("my saga 1"));
        }
    }
}

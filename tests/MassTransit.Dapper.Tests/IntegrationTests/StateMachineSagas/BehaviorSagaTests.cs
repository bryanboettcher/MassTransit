namespace MassTransit.Dapper.Tests.IntegrationTests.StateMachineSagas
{
    using System.Threading.Tasks;
    using ConsumerSagas;
    using MassTransit.Dapper.Integration.Saga;
    using MassTransit.Dapper.SqlServer.Configuration;
    using MassTransit.Dapper.Tests.Common;
    using MassTransit.Testing;
    using NUnit.Framework;


    [Category("Integration")]
    [TestFixture]
    public class BehaviorSagaTests : DapperVersionedSagaTests
    {
        readonly VersionedSagaStateMachine _stateMachine;
        ISagaRepository<VersionedBehaviorSaga> _repository;

        public BehaviorSagaTests() => _stateMachine = new VersionedSagaStateMachine();

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _repository = AdoSagaRepository<VersionedBehaviorSaga>.Create(conf =>
                conf.UsingSqlServer(
                    ConnectionString, sql => sql.SetTableName("VersionedSagas").SetOptimisticConcurrency()
                )
            );

            configurator.StateMachineSaga(_stateMachine, _repository);
            base.ConfigureInMemoryReceiveEndpoint(configurator);
        }

        [Test]
        public async Task CreateMessage_creates_saga()
        {
            var sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Empty);

            await InputQueueSendEndpoint.Send<CreateSaga>(new { CorrelationId = SagaId, Name = "my saga" });
            await BusTestHarness.Consumed.Any<CreateSaga>();

            var found = await _repository.ShouldContainSaga(SagaId, DefaultTimeout);
            Assert.That(found, Is.EqualTo(SagaId));

            sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Not.Empty);
            Assert.That(sagas[0].Name, Is.EqualTo("my saga"));
        }

        [Test]
        public async Task UpdateMessage_updates_saga()
        {
            var sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Empty);

            await InputQueueSendEndpoint.Send<CreateSaga>(new { CorrelationId = SagaId, Name = "my saga 0" });
            await BusTestHarness.Consumed.Any<CreateSaga>();
            await Task.Delay(50);

            await InputQueueSendEndpoint.Send<UpdateSaga>(new { CorrelationId = SagaId, Name = "my saga 1" });
            await BusTestHarness.Consumed.Any<UpdateSaga>();

            var found = await _repository.ShouldContainSaga(SagaId, DefaultTimeout);
            Assert.That(found, Is.EqualTo(SagaId));

            sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Not.Empty);
            Assert.That(sagas[0].Name, Is.EqualTo("my saga 1"));
        }

        [Test]
        public async Task DeleteMessage_deletes_saga()
        {
            var sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Empty);

            await InputQueueSendEndpoint.Send<CreateSaga>(new { CorrelationId = SagaId, Name = "my saga" });
            await BusTestHarness.Consumed.Any<CreateSaga>();

            var found = await _repository.ShouldContainSaga(SagaId, DefaultTimeout);
            Assert.That(found, Is.EqualTo(SagaId));

            await InputQueueSendEndpoint.Send<DeleteSagaByName>(new { Name = "my saga" });
            await BusTestHarness.Consumed.Any<DeleteSagaByName>();

            sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Empty);
        }
    }
}

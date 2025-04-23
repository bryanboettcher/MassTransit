namespace MassTransit.DapperIntegration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Dapper;
    using MassTransit.Tests;
    using Microsoft.Data.SqlClient;
    using NUnit.Framework;
    using Saga;
    using TestFramework;
    using Testing;


    public abstract class DapperVersionedSagaTests : InMemoryTestFixture
    {
        protected readonly string ConnectionString;
        protected readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);
        
        protected static readonly Guid SagaId1 = Guid.Parse("d747db39-0d64-49b5-85f4-2a796ba82130");
        protected static readonly Guid SagaId2 = Guid.Parse("e80f0da3-bc55-4252-b1b5-bf9c5024ebf9");

        protected DapperVersionedSagaTests()
        {
            ConnectionString = LocalDbConnectionStringProvider.GetLocalDbConnectionString();
        }
        
        [OneTimeSetUp]
        public async Task Initialize()
        {
            await using var connection = new SqlConnection(ConnectionString);
            var sql = @"DROP TABLE IF EXISTS VersionedSagas;

CREATE TABLE VersionedSagas (
    [CorrelationId] UNIQUEIDENTIFIER NOT NULL,
    [Version] INT NOT NULL,
    [CurrentState] VARCHAR(20),

    [Name] NVARCHAR(MAX),
    
    PRIMARY KEY CLUSTERED (CorrelationId)
);";

            await connection.ExecuteAsync(sql);
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            await using var connection = new SqlConnection(ConnectionString);
            var sql = @"DROP TABLE IF EXISTS VersionedSagas;";
            //await connection.ExecuteAsync(sql);
        }

        [SetUp]
        public async Task Setup()
        {
            await using var connection = new SqlConnection(ConnectionString);
            var sql = @"TRUNCATE TABLE VersionedSagas;";
            await connection.ExecuteAsync(sql);
        }

        protected async Task<List<TSaga>> GetSagas<TSaga>() where TSaga : class, ISaga
        {
            await using var connection = new SqlConnection(ConnectionString);
            var sql = "SELECT * FROM VersionedSagas;";
            return (await connection.QueryAsync<TSaga>(sql)).AsList();
        }
    }


    public class ConsumerSagaTests : DapperVersionedSagaTests
    {
        ISagaRepository<VersionedConsumerSaga> _repository;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _repository = DapperSagaRepository<VersionedConsumerSaga>.Create(ConnectionString, conf =>
            {
                conf.ContextFactory = (c, t) => new SagaDatabaseContext<VersionedConsumerSaga>(c, t, "VersionedSagas");
            });

            configurator.Saga(_repository);
            base.ConfigureInMemoryReceiveEndpoint(configurator);
        }

        [Test]
        public async Task CreateMessage_creates_saga()
        {
            await InputQueueSendEndpoint.Send<CreateSaga>(new { CorrelationId = SagaId1, Name = "my saga" });

            var found = await _repository.ShouldContainSaga(SagaId1, DefaultTimeout);
            Assert.That(found, Is.EqualTo(SagaId1));

            var sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Not.Empty);
            Assert.That(sagas.Count, Is.EqualTo(1));
            Assert.That(sagas[0].Name, Is.EqualTo("my saga"));
            Assert.That(sagas[0].Version, Is.EqualTo(1));
        }
        
        [Test]
        public async Task UpdateMessage_updates_saga()
        {
            await InputQueueSendEndpoint.Send<CreateSaga>(new { CorrelationId = SagaId1, Name = "my saga 0" });
            await InputQueueSendEndpoint.Send<UpdateSaga>(new { CorrelationId = SagaId1, Name = "my saga 1" });

            var found = await _repository.ShouldContainSaga(SagaId1, DefaultTimeout);
            Assert.That(found, Is.EqualTo(SagaId1));

            var sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Not.Empty);
            Assert.That(sagas.Count, Is.EqualTo(1));
            Assert.That(sagas[0].Name, Is.EqualTo("my saga 1"));
            Assert.That(sagas[0].Version, Is.EqualTo(2));
        }
    }

    public class BehaviorSagaTests : DapperVersionedSagaTests
    {
        readonly VersionedSagaStateMachine _stateMachine;
        ISagaRepository<VersionedBehaviorSaga> _repository;

        public BehaviorSagaTests() => _stateMachine = new VersionedSagaStateMachine();

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _repository = DapperSagaRepository<VersionedBehaviorSaga>.Create(ConnectionString, conf =>
            {
                conf.ContextFactory = (c, t) => new SagaDatabaseContext<VersionedBehaviorSaga>(c, t, "VersionedSagas");
            });

            configurator.StateMachineSaga(_stateMachine, _repository);
            base.ConfigureInMemoryReceiveEndpoint(configurator);
        }

        [Test]
        public async Task CreateMessage_creates_saga()
        {
            await InputQueueSendEndpoint.Send<CreateSaga>(new { CorrelationId = SagaId1, Name = "my saga" });

            var found = await _repository.ShouldContainSaga(SagaId1, DefaultTimeout);
            Assert.That(found, Is.EqualTo(SagaId1));

            var sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Not.Empty);
            Assert.That(sagas[0].Name, Is.EqualTo("my saga"));
            Assert.That(sagas[0].Version, Is.EqualTo(1));
        }

        [Test]
        public async Task UpdateMessage_updates_saga()
        {
            await InputQueueSendEndpoint.Send<CreateSaga>(new { CorrelationId = SagaId1, Name = "my saga 0" });
            await InputQueueSendEndpoint.Send<UpdateSaga>(new { CorrelationId = SagaId1, Name = "my saga 1" });

            var found = await _repository.ShouldContainSaga(SagaId1, DefaultTimeout);
            Assert.That(found, Is.EqualTo(SagaId1));

            var sagas = await GetSagas<VersionedConsumerSaga>();
            Assert.That(sagas, Is.Not.Empty);
            Assert.That(sagas[0].Name, Is.EqualTo("my saga 1"));
            Assert.That(sagas[0].Version, Is.EqualTo(2));
        }
    }

    public class VersionedConsumerSaga : ISagaVersion,
        InitiatedBy<CreateSaga>,
        Orchestrates<UpdateSaga>
    {
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
        public string CurrentState { get; set; }
        public string Name { get; set; }

        public async Task Consume(ConsumeContext<CreateSaga> context)
        {
            Name = context.Message.Name;
            CurrentState = "Ready";
        }

        public async Task Consume(ConsumeContext<UpdateSaga> context)
        {
            Name = context.Message.Name;
        }
    }

    public class VersionedBehaviorSaga : SagaStateMachineInstance,
        ISagaVersion
    {
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
        public string CurrentState { get; set; }
        public string Name { get; set; }
    }

    public class VersionedSagaStateMachine : MassTransitStateMachine<VersionedBehaviorSaga>
    {
        public VersionedSagaStateMachine()
        {
            InstanceState(c => c.CurrentState);

            Initially(
                When(OnCreate)
                    .Then(ctx => ctx.Saga.Name = ctx.Message.Name)
                    .TransitionTo(Ready)
            );

            During(Ready,
                When(OnUpdate)
                    .Then(ctx => ctx.Saga.Name = ctx.Message.Name));
        }

        public State Ready { get; } = null!;

        public Event<CreateSaga> OnCreate { get; } = null!;
        public Event<UpdateSaga> OnUpdate { get; } = null!;
    }

    public interface CreateSaga : CorrelatedBy<Guid> { string Name { get; } }
    public interface UpdateSaga : CorrelatedBy<Guid> { string Name { get; } }
}

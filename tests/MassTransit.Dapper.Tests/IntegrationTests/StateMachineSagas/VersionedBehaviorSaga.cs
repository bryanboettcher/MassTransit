namespace MassTransit.Dapper.Tests.IntegrationTests.StateMachineSagas
{
    using System;


    public class VersionedBehaviorSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public byte[] RowVersion { get; set; }
        public string CurrentState { get; set; }
        public string Name { get; set; }
    }
}

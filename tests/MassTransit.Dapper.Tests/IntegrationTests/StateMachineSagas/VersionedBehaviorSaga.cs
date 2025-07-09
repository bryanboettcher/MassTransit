namespace MassTransit.Dapper.Tests.IntegrationTests.StateMachineSagas
{
    using System;


    public abstract class BehaviorSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public string Name { get; set; }
    }

}

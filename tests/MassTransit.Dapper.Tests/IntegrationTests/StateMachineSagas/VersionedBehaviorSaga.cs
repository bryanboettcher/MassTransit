namespace MassTransit.Dapper.Tests.IntegrationTests.StateMachineSagas
{
    public abstract class BehaviorSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }
        public string Name { get; set; }
    }

}

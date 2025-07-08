// ReSharper disable UnassignedGetOnlyAutoProperty
// Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8618
namespace MassTransit.Dapper.Tests.IntegrationTests.StateMachineSagas
{
    using MassTransit.Dapper.Tests.Common;


    public class VersionedSagaStateMachine : MassTransitStateMachine<VersionedBehaviorSaga>
    {
        public VersionedSagaStateMachine()
        {
            InstanceState(c => c.CurrentState);

            Event(() => OnDelete, e => e.CorrelateBy((s, c) => s.Name == c.Message.Name));

            Initially(
                When(OnCreate)
                    .Then(ctx => ctx.Saga.Name = ctx.Message.Name)
                    .TransitionTo(Ready)
            );

            During(Ready,
                When(OnUpdate)
                    .Then(ctx => ctx.Saga.Name = ctx.Message.Name),
                When(OnDelete)
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        public State Ready { get; }

        public Event<CreateSaga> OnCreate { get; }
        public Event<UpdateSaga> OnUpdate { get; }
        public Event<DeleteSagaByName> OnDelete { get; }
    }
}

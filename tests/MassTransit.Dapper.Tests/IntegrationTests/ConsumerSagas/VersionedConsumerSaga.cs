namespace MassTransit.Dapper.Tests.IntegrationTests.ConsumerSagas
{
    using System;
    using System.Threading.Tasks;
    using MassTransit.Dapper.Tests.Common;


    public class VersionedConsumerSaga : ISaga,
        InitiatedBy<CreateSaga>,
        Orchestrates<UpdateSaga>
    {
        public Guid CorrelationId { get; set; }
        public byte[] RowVersion { get; set; }
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
}

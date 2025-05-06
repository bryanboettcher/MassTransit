namespace MassTransit.DapperIntegration.Tests.IntegrationTests
{
    using System;


    public interface CreateSaga : CorrelatedBy<Guid> { string Name { get; } }
}

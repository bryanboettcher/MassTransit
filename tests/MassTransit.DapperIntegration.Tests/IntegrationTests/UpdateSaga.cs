namespace MassTransit.DapperIntegration.Tests.IntegrationTests
{
    using System;


    public interface UpdateSaga : CorrelatedBy<Guid> { string Name { get; } }
}

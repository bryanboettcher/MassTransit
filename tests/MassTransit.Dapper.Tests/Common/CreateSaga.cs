namespace MassTransit.Dapper.Tests.Common
{
    using System;


    public interface CreateSaga : CorrelatedBy<Guid> { string Name { get; } }
}

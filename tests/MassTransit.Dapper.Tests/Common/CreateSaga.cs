namespace MassTransit.Dapper.Tests.Common
{
    public interface CreateSaga : CorrelatedBy<Guid> { string Name { get; } }
}

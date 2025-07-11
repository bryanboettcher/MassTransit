namespace MassTransit.Dapper.Tests.Common
{
    public interface UpdateSaga : CorrelatedBy<Guid> { string Name { get; } }
}

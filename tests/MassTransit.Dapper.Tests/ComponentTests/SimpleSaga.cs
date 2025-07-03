namespace MassTransit.DapperIntegration.Tests.ComponentTests;

public class SimpleSaga
{
    public Guid CorrelationId { get; set; }
    public string CorrelateBySomething { get; set; }
    public bool Completed { get; set; }
}

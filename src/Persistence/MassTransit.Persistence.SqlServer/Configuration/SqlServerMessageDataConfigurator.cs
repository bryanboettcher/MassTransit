namespace MassTransit.Persistence.SqlServer.Configuration;

using System.Data;


public class SqlServerMessageDataConfigurator : ISqlServerMessageDataConfigurator, ISpecification
{
    /// <inheritdoc />
    public string ConnectionString { get; set; }

    /// <inheritdoc />
    public string TableName { get; set; } = "ClaimChecks";

    /// <inheritdoc />
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;

    /// <inheritdoc />
    public ISqlServerMessageDataConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    /// <inheritdoc />
    public ISqlServerMessageDataConfigurator SetTableName(string tableName)
    {
        TableName = tableName;
        return this;
    }

    /// <inheritdoc />
    public ISqlServerMessageDataConfigurator SetIsolationLevel(IsolationLevel isolationLevel)
    {
        IsolationLevel = isolationLevel;
        return this;
    }
    
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure($"{nameof(ConnectionString)} must be set");

        if (string.IsNullOrWhiteSpace(TableName))
            yield return this.Failure($"{nameof(TableName)} must be set");
    }
}

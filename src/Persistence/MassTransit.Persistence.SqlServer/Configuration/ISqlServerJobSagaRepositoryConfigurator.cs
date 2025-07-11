namespace MassTransit.Persistence.SqlServer.Configuration;

/// <summary>
/// Builds the appropriate components for a SQL Server-based DatabaseContext, specific to JobConsumers.
/// </summary>
public interface ISqlServerJobSagaRepositoryConfigurator
{
    /// <summary>
    /// Sets the connection string used by all JobConsumer sagas.
    /// </summary>
    ISqlServerJobSagaRepositoryConfigurator SetConnectionString(string connectionString);

    /// <summary>
    /// Gets/sets the connection string used by all JobConsumer sagas.
    /// </summary>
    string ConnectionString { get; set; }
}

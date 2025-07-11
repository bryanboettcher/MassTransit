namespace MassTransit.Persistence.MySql.Configuration;

/// <summary>
/// Builds the appropriate components for a MySql-based DatabaseContext, specific to JobConsumers.
/// </summary>
public interface IMySqlJobSagaRepositoryConfigurator
{
    /// <summary>
    /// Sets the connection string used by all JobConsumer sagas.
    /// </summary>
    IMySqlJobSagaRepositoryConfigurator SetConnectionString(string connectionString);

    /// <summary>
    /// Gets/sets the connection string used by all JobConsumer sagas.
    /// </summary>
    string ConnectionString { get; set; }
}

namespace MassTransit.Dapper.PostgreSql.Configuration;

/// <summary>
/// Builds the appropriate components for a Postgres-based DatabaseContext, specific to JobConsumers.
/// </summary>
public interface IPostgresJobSagaRepositoryConfigurator
{
    /// <summary>
    /// Sets the connection string used by all JobConsumer sagas.
    /// </summary>
    IPostgresJobSagaRepositoryConfigurator SetConnectionString(string connectionString);

    /// <summary>
    /// Gets/sets the connection string used by all JobConsumer sagas.
    /// </summary>
    string ConnectionString { get; set; }
}

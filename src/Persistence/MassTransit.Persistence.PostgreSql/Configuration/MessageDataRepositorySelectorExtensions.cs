namespace MassTransit.Persistence.PostgreSql.Configuration;

using System.Data;
using ClaimChecks;
using MassTransit.Configuration;
using Npgsql;


public static class MessageDataRepositorySelectorExtensions
{
    /// <summary>
    /// Configures a MessageData repository using Postgres.  Requires
    /// setting the connection string via <paramref name="configure"/>.
    /// </summary>
    public static IMessageDataRepository UsingPostgres(
        this IMessageDataRepositorySelector selector,
        Action<IPostgresMessageDataConfigurator> configure)
    {
        return UsingPostgres(selector, string.Empty, configure);
    }

    /// <summary>
    /// Configures a MessageData repository using Postgres.
    /// Builds the connection string from the parameters.
    /// </summary>
    public static IMessageDataRepository UsingPostgres(
        this IMessageDataRepositorySelector selector,
        string hostname, string catalog, string username, string password,
        Action<IPostgresMessageDataConfigurator>? configure = null)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = hostname,
            Database = catalog,
            Username = username,
            Password = password
        };

        return UsingPostgres(selector, connectionStringBuilder.ToString());
    }

    /// <summary>
    /// Configures a MessageData repository using Postgres.  Requires
    /// passing the connection string.
    /// </summary>
    public static IMessageDataRepository UsingPostgres(
        this IMessageDataRepositorySelector selector,
        string connectionString,
        Action<IPostgresMessageDataConfigurator>? configure = null)
    {
        var configurator = new PostgresMessageDataConfigurator();

        configurator.SetConnectionString(connectionString);

        configure?.Invoke(configurator);

        configurator.Validate().ThrowIfContainsFailure("The Sql Server configuration is invalid:");
        
        return new PostgresMessageDataRepository(
            configurator.ConnectionString,
            configurator.TableName,
            configurator.IsolationLevel,
            TimeProvider.System
        );
    }
}

public interface IPostgresMessageDataConfigurator
{
    /// <summary>
    /// Sets the connection string.
    /// </summary>
    IPostgresMessageDataConfigurator SetConnectionString(string connectionString);

    /// <summary>
    /// Sets the table name.
    /// </summary>

    IPostgresMessageDataConfigurator SetTableName(string tableName);

    /// <summary>
    /// Sets the isolation level.
    /// </summary>
    IPostgresMessageDataConfigurator SetIsolationLevel(IsolationLevel isolationLevel);

    /// <summary>
    /// Gets/sets the connection string.
    /// </summary>
    string ConnectionString { get; set; }

    /// <summary>
    /// Gets/sets the table name.  Defaults to ClaimChecks.
    /// </summary>
    string TableName { get; set; }

    /// <summary>
    /// Gets/sets the isolation level used during requests.
    /// </summary>
    IsolationLevel IsolationLevel { get; set; }
}

public class PostgresMessageDataConfigurator : IPostgresMessageDataConfigurator, ISpecification
{
    /// <inheritdoc />
    public string ConnectionString { get; set; }

    /// <inheritdoc />
    public string TableName { get; set; } = "ClaimChecks";

    /// <inheritdoc />
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;

    /// <inheritdoc />
    public IPostgresMessageDataConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    /// <inheritdoc />
    public IPostgresMessageDataConfigurator SetTableName(string tableName)
    {
        TableName = tableName;
        return this;
    }

    /// <inheritdoc />
    public IPostgresMessageDataConfigurator SetIsolationLevel(IsolationLevel isolationLevel)
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

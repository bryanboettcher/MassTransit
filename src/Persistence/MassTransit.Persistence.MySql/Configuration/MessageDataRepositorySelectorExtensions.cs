namespace MassTransit.Dapper.SqlServer.Configuration;

using System.Data;
using ClaimChecks;
using global::MySql.Data.MySqlClient;
using MassTransit.Configuration;

public static class MessageDataRepositorySelectorExtensions
{
    /// <summary>
    /// Configures a MessageData repository using MySql.  Requires
    /// setting the connection string via <paramref name="configure"/>.
    /// </summary>
    public static IMessageDataRepository UsingMySql(
        this IMessageDataRepositorySelector selector,
        Action<IMySqlMessageDataConfigurator> configure)
    {
        return UsingMySql(selector, string.Empty, configure);
    }

    /// <summary>
    /// Configures a MessageData repository using MySql.
    /// Builds the connection string from the parameters.
    /// </summary>
    public static IMessageDataRepository UsingMySql(
        this IMessageDataRepositorySelector selector,
        string hostname, string catalog, string username, string password,
        Action<IMySqlMessageDataConfigurator>? configure = null)
    {
        var connectionStringBuilder = new MySqlConnectionStringBuilder
        {
            Server = hostname,
            Database = catalog,
            UserID = username,
            Password = password
        };

        return UsingMySql(selector, connectionStringBuilder.ToString());
    }

    /// <summary>
    /// Configures a MessageData repository using MySql.  Requires
    /// passing the connection string.
    /// </summary>
    public static IMessageDataRepository UsingMySql(
        this IMessageDataRepositorySelector selector,
        string connectionString,
        Action<IMySqlMessageDataConfigurator>? configure = null)
    {
        var configurator = new MySqlMessageDataConfigurator();

        configurator.SetConnectionString(connectionString);

        configure?.Invoke(configurator);

        configurator.Validate().ThrowIfContainsFailure("The Sql Server configuration is invalid:");
        
        return new MySqlMessageDataRepository(
            configurator.ConnectionString,
            configurator.TableName,
            configurator.IsolationLevel,
            TimeProvider.System
        );
    }
}


public interface IMySqlMessageDataConfigurator
{
    /// <summary>
    /// Sets the connection string.
    /// </summary>
    IMySqlMessageDataConfigurator SetConnectionString(string connectionString);

    /// <summary>
    /// Sets the table name.
    /// </summary>

    IMySqlMessageDataConfigurator SetTableName(string tableName);

    /// <summary>
    /// Sets the isolation level.
    /// </summary>
    IMySqlMessageDataConfigurator SetIsolationLevel(IsolationLevel isolationLevel);

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

public class MySqlMessageDataConfigurator : IMySqlMessageDataConfigurator, ISpecification
{
    /// <inheritdoc />
    public string ConnectionString { get; set; }

    /// <inheritdoc />
    public string TableName { get; set; } = "ClaimChecks";

    /// <inheritdoc />
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;

    /// <inheritdoc />
    public IMySqlMessageDataConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    /// <inheritdoc />
    public IMySqlMessageDataConfigurator SetTableName(string tableName)
    {
        TableName = tableName;
        return this;
    }

    /// <inheritdoc />
    public IMySqlMessageDataConfigurator SetIsolationLevel(IsolationLevel isolationLevel)
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

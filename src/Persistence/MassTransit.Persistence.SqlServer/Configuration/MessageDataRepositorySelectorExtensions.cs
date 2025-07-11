namespace MassTransit.Persistence.SqlServer.Configuration;

using System.Data;
using ClaimChecks;
using MassTransit.Configuration;
using Microsoft.Data.SqlClient;


public static class MessageDataRepositorySelectorExtensions
{
    /// <summary>
    /// Configures a MessageData repository using SqlServer.  Requires
    /// setting the connection string via <paramref name="configure"/>.
    /// </summary>
    public static IMessageDataRepository UsingSqlServer(
        this IMessageDataRepositorySelector selector,
        Action<ISqlServerMessageDataConfigurator> configure)
    {
        return UsingSqlServer(selector, string.Empty, configure);
    }

    /// <summary>
    /// Configures a MessageData repository using SqlServer.
    /// Builds the connection string from the parameters.
    /// </summary>
    public static IMessageDataRepository UsingSqlServer(
        this IMessageDataRepositorySelector selector,
        string hostname, string catalog, string username, string password,
        Action<ISqlServerMessageDataConfigurator>? configure = null)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder
        {
            DataSource = hostname,
            InitialCatalog = catalog,
            UserID = username,
            Password = password
        };

        return UsingSqlServer(selector, connectionStringBuilder.ToString());
    }

    /// <summary>
    /// Configures a MessageData repository using SqlServer.  Requires
    /// passing the connection string.
    /// </summary>
    public static IMessageDataRepository UsingSqlServer(
        this IMessageDataRepositorySelector selector,
        string connectionString,
        Action<ISqlServerMessageDataConfigurator>? configure = null)
    {
        var configurator = new SqlServerMessageDataConfigurator();

        configurator.SetConnectionString(connectionString);

        configure?.Invoke(configurator);

        configurator.Validate().ThrowIfContainsFailure("The Sql Server configuration is invalid:");
        
        return new SqlServerMessageDataRepository(
            configurator.ConnectionString,
            configurator.TableName,
            configurator.IsolationLevel,
            TimeProvider.System
        );
    }
}


public interface ISqlServerMessageDataConfigurator
{
    /// <summary>
    /// Sets the connection string.
    /// </summary>
    ISqlServerMessageDataConfigurator SetConnectionString(string connectionString);

    /// <summary>
    /// Sets the table name.
    /// </summary>

    ISqlServerMessageDataConfigurator SetTableName(string tableName);

    /// <summary>
    /// Sets the isolation level.
    /// </summary>
    ISqlServerMessageDataConfigurator SetIsolationLevel(IsolationLevel isolationLevel);

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

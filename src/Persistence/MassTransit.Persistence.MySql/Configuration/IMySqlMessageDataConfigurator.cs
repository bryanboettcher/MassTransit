namespace MassTransit.Persistence.MySql.Configuration;

using System.Data;


/// <summary>
/// Configures a MessageData repository using MySql.
/// </summary>
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

namespace MassTransit.Dapper.SqlServer.Configuration;

using ClaimChecks;
using MassTransit.Configuration;
using Microsoft.Data.SqlClient;


public static class MessageDataRepositorySelectorExtensions
{
    public static IMessageDataRepository UsingSqlServer(
        this IMessageDataRepositorySelector selector,
        Action<ISqlServerMessageDataConfigurator> configure)
    {
        return UsingSqlServer(selector, string.Empty, configure);
    }

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
            configurator.TableName
        );
    }
}

public interface ISqlServerMessageDataConfigurator
{}

public class SqlServerMessageDataConfigurator : ISqlServerMessageDataConfigurator, ISpecification
{
    public string ConnectionString { get; set; }
    public string TableName { get; set; } = "ClaimChecks";

    public ISqlServerMessageDataConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    public ISqlServerMessageDataConfigurator SetTableName(string tableName)
    {
        TableName = tableName;
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

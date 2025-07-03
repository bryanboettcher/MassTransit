namespace MassTransit.Dapper.SqlServer.Configuration;

using Dapper.Configuration;
using Microsoft.Data.SqlClient;


public static class DapperJobSagaRepositoryConfiguratorExtensions
{
    public static IDapperJobSagaRepositoryConfigurator UsingSqlServer(
        this IDapperJobSagaRepositoryConfigurator jobSagaConfigurator,
        Action<ISqlServerJobSagaRepositoryConfigurator> configure)
    {
        var repositoryConfigurator = new SqlServerJobSagaRepositoryConfigurator();

        configure.Invoke(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The Sql Server configuration is invalid:");
        repositoryConfigurator.Configure(jobSagaConfigurator);

        return jobSagaConfigurator;
    }

    public static IDapperJobSagaRepositoryConfigurator UsingSqlServer(
        this IDapperJobSagaRepositoryConfigurator jobSagaConfigurator,
        string hostname, string catalog, string username, string password,
        Action<ISqlServerJobSagaRepositoryConfigurator>? configure = null)
    {
        var repositoryConfigurator = new SqlServerJobSagaRepositoryConfigurator();

        var connectionStringBuilder = new SqlConnectionStringBuilder
        {
            DataSource = hostname,
            InitialCatalog = catalog,
            UserID = username,
            Password = password
        };

        repositoryConfigurator.SetConnectionString(connectionStringBuilder.ToString());

        configure?.Invoke(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The Sql Server configuration is invalid:");
        repositoryConfigurator.Configure(jobSagaConfigurator);

        return jobSagaConfigurator;
    }

    public static IDapperJobSagaRepositoryConfigurator UsingSqlServer(
        this IDapperJobSagaRepositoryConfigurator jobSagaConfigurator,
        string connectionString,
        Action<ISqlServerJobSagaRepositoryConfigurator>? configure = null)
    {
        var repositoryConfigurator = new SqlServerJobSagaRepositoryConfigurator();

        repositoryConfigurator.SetConnectionString(connectionString);

        configure?.Invoke(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The Sql Server configuration is invalid:");
        repositoryConfigurator.Configure(jobSagaConfigurator);

        return jobSagaConfigurator;
    }
}


public interface ISqlServerJobSagaRepositoryConfigurator
{
    ISqlServerJobSagaRepositoryConfigurator SetConnectionString(string connectionString);
    
    string ConnectionString { get; set; }
}

public class SqlServerJobSagaRepositoryConfigurator : ISqlServerJobSagaRepositoryConfigurator, ISpecification
{
    public string? ConnectionString { get; set; }
    
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure("ConnectionString must be specified");
    }

    public void Configure(IDapperJobSagaRepositoryConfigurator jobSagaConfigurator)
    {
    }

    public ISqlServerJobSagaRepositoryConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }
}

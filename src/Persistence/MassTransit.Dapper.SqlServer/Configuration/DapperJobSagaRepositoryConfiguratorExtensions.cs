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
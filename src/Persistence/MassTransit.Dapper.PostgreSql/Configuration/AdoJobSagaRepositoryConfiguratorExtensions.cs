namespace MassTransit.Dapper.PostgreSql.Configuration;

using MassTransit.Dapper.Configuration;
using Npgsql;


public static class AdoJobSagaRepositoryConfiguratorExtensions
{
    public static IAdoJobSagaRepositoryConfigurator UsingPostgres(
        this IAdoJobSagaRepositoryConfigurator jobSagaConfigurator,
        Action<IPostgresJobSagaRepositoryConfigurator> configure)
    {
        var repositoryConfigurator = new PostgresJobSagaRepositoryConfigurator();

        configure.Invoke(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The PostgreSql configuration is invalid:");
        repositoryConfigurator.Configure(jobSagaConfigurator);

        return jobSagaConfigurator;
    }

    public static IAdoJobSagaRepositoryConfigurator UsingPostgres(
        this IAdoJobSagaRepositoryConfigurator jobSagaConfigurator,
        string hostname, string catalog, string username, string password,
        Action<IPostgresJobSagaRepositoryConfigurator>? configure = null)
    {
        var repositoryConfigurator = new PostgresJobSagaRepositoryConfigurator();

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = hostname,
            Database = catalog,
            Username = username,
            Password = password
        };

        repositoryConfigurator.SetConnectionString(connectionStringBuilder.ToString());

        configure?.Invoke(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The PostgreSql configuration is invalid:");
        repositoryConfigurator.Configure(jobSagaConfigurator);

        return jobSagaConfigurator;
    }

    public static IAdoJobSagaRepositoryConfigurator UsingPostgres(
        this IAdoJobSagaRepositoryConfigurator jobSagaConfigurator,
        string connectionString,
        Action<IPostgresJobSagaRepositoryConfigurator>? configure = null)
    {
        var repositoryConfigurator = new PostgresJobSagaRepositoryConfigurator();

        repositoryConfigurator.SetConnectionString(connectionString);

        configure?.Invoke(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The PostgreSql configuration is invalid:");
        repositoryConfigurator.Configure(jobSagaConfigurator);

        return jobSagaConfigurator;
    }
}

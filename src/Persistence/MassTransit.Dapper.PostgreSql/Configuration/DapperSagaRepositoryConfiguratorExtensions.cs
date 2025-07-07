namespace MassTransit.Dapper.PostgreSql.Configuration
{
    using MassTransit;
    using MassTransit.Dapper.Configuration;
    using Npgsql;


    public static class DapperSagaRepositoryConfiguratorExtensions
    {
        public static IDapperRepositoryConfigurator<TSaga> UsingPostgres<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> sagaConfigurator,
            Action<IPostgresRepositoryConfigurator<TSaga>> configure) where TSaga : class, ISaga
        {
            var repositoryConfigurator = new PostgresRepositoryConfigurator<TSaga>();

            configure.Invoke(repositoryConfigurator);
            repositoryConfigurator.Validate().ThrowIfContainsFailure("The PostgreSql configuration is invalid:");
            repositoryConfigurator.Configure(sagaConfigurator);

            return sagaConfigurator;
        }

        public static IDapperRepositoryConfigurator<TSaga> UsingPostgres<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> sagaConfigurator,
            string hostname, string catalog, string username, string password,
            Action<IPostgresRepositoryConfigurator<TSaga>>? configure = null)
            where TSaga : class, ISaga
        {
            var repositoryConfigurator = new PostgresRepositoryConfigurator<TSaga>();
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
            repositoryConfigurator.Configure(sagaConfigurator);
            return sagaConfigurator;
        }

        public static IDapperRepositoryConfigurator<TSaga> UsingPostgres<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> sagaConfigurator,
            string connectionString,
            Action<IPostgresRepositoryConfigurator<TSaga>>? configure = null)
            where TSaga : class, ISaga
        {
            var repositoryConfigurator = new PostgresRepositoryConfigurator<TSaga>();

            repositoryConfigurator.SetConnectionString(connectionString);

            configure?.Invoke(repositoryConfigurator);

            repositoryConfigurator.Validate().ThrowIfContainsFailure("The PostgreSql configuration is invalid:");
            repositoryConfigurator.Configure(sagaConfigurator);

            return sagaConfigurator;
        }
    }
}

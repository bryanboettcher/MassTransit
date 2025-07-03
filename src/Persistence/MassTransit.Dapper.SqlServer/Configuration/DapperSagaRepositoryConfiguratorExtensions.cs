namespace MassTransit.Dapper.SqlServer.Configuration
{
    using Dapper.Configuration;
    using MassTransit;
    using Microsoft.Data.SqlClient;
    
    public static class DapperSagaRepositoryConfiguratorExtensions
    {
        public static IDapperRepositoryConfigurator<TSaga> UsingSqlServer<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> sagaConfigurator,
            Action<ISqlServerRepositoryConfigurator<TSaga>> configure) where TSaga : class, ISaga
        {
            var repositoryConfigurator = new SqlServerRepositoryConfigurator<TSaga>();

            configure.Invoke(repositoryConfigurator);
            repositoryConfigurator.Validate().ThrowIfContainsFailure("The Sql Server configuration is invalid:");
            repositoryConfigurator.Configure(sagaConfigurator);

            return sagaConfigurator;
        }

        public static IDapperRepositoryConfigurator<TSaga> UsingSqlServer<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> sagaConfigurator,
            string hostname, string catalog, string username, string password,
            Action<ISqlServerRepositoryConfigurator<TSaga>>? configure = null)
            where TSaga : class, ISaga
        {
            var repositoryConfigurator = new SqlServerRepositoryConfigurator<TSaga>();
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
            repositoryConfigurator.Configure(sagaConfigurator);
            return sagaConfigurator;
        }

        public static IDapperRepositoryConfigurator<TSaga> UsingSqlServer<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> sagaConfigurator,
            string connectionString,
            Action<ISqlServerRepositoryConfigurator<TSaga>>? configure = null)
            where TSaga : class, ISaga
        {
            var repositoryConfigurator = new SqlServerRepositoryConfigurator<TSaga>();

            repositoryConfigurator.SetConnectionString(connectionString);

            configure?.Invoke(repositoryConfigurator);

            repositoryConfigurator.Validate().ThrowIfContainsFailure("The Sql Server configuration is invalid:");
            repositoryConfigurator.Configure(sagaConfigurator);

            return sagaConfigurator;
        }
    }
}

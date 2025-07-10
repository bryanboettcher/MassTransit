namespace MassTransit.Dapper.MySql.Configuration
{
    using global::MySql.Data.MySqlClient;
    using MassTransit;
    using MassTransit.Dapper.Configuration;

    public static class AdoSagaRepositoryConfiguratorExtensions
    {
        /// <summary>
        /// Configures a MySql-based saga repository for this <typeparamref name="TSaga"/>.
        /// Requires setting the connection string as part of the <paramref name="configure"/>
        /// callback.
        /// </summary>
        public static IAdoRepositoryConfigurator<TSaga> UsingMySql<TSaga>(
            this IAdoRepositoryConfigurator<TSaga> sagaConfigurator,
            Action<IMySqlRepositoryConfigurator<TSaga>> configure) where TSaga : class, ISaga
        {
            return UsingMySql(sagaConfigurator, string.Empty, configure);
        }

        /// <summary>
        /// Configures a MySql-based saga repository for this <typeparamref name="TSaga"/>.
        /// Builds the connection string from the parameters.
        /// </summary>
        public static IAdoRepositoryConfigurator<TSaga> UsingMySql<TSaga>(
            this IAdoRepositoryConfigurator<TSaga> sagaConfigurator,
            string hostname, string catalog, string username, string password,
            Action<IMySqlRepositoryConfigurator<TSaga>>? configure = null)
            where TSaga : class, ISaga
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder
            {
                Server = hostname,
                Database = catalog,
                UserID = username,
                Password = password
            };

            return UsingMySql(sagaConfigurator, connectionStringBuilder.ToString(), configure);
        }

        /// <summary>
        /// Configures a MySql-based saga repository for this <typeparamref name="TSaga"/>.
        /// Takes the connection string directly.
        /// </summary>
        public static IAdoRepositoryConfigurator<TSaga> UsingMySql<TSaga>(
            this IAdoRepositoryConfigurator<TSaga> sagaConfigurator,
            string connectionString,
            Action<IMySqlRepositoryConfigurator<TSaga>>? configure = null)
            where TSaga : class, ISaga
        {
            var repositoryConfigurator = new MySqlRepositoryConfigurator<TSaga>();

            repositoryConfigurator.SetConnectionString(connectionString);

            configure?.Invoke(repositoryConfigurator);

            repositoryConfigurator.Validate().ThrowIfContainsFailure("The MySql configuration is invalid:");
            repositoryConfigurator.Configure(sagaConfigurator);

            return sagaConfigurator;
        }
    }
}

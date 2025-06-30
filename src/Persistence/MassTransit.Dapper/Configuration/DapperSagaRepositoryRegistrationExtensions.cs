#nullable enable
namespace MassTransit
{
    using System;


    public static partial class DapperSagaRepositoryRegistrationExtensions
    {
        /// <summary>
        /// Wires this saga to use the Dapper repository provider.
        /// </summary>
        /// <param name="configurator">The saga registration configurator</param>
        /// <param name="configure">The saga registration callback</param>
        public static ISagaRegistrationConfigurator<TSaga> DapperRepository<TSaga>(this ISagaRegistrationConfigurator<TSaga> configurator,
            Action<IDapperSagaRepositoryConfigurator<TSaga>> configure = null)
            where TSaga : class, ISaga
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return DapperRepository(configurator, string.Empty, configure);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Wires this saga to use the Dapper repository provider, with a shortcut to set the connection string.  This method
        /// only allows for SQL Server and is far less configurable.
        /// </summary>
        /// <param name="configurator">The saga registration configurator</param>
        /// <param name="connectionString"></param>
        /// <param name="configure">The saga registration callback</param>
        [Obsolete("DapperRepository should use the configure-only method", false)]
        public static ISagaRegistrationConfigurator<TSaga> DapperRepository<TSaga>(this ISagaRegistrationConfigurator<TSaga> configurator,
            string connectionString, Action<IDapperSagaRepositoryConfigurator<TSaga>> configure = null)
            where TSaga : class, ISaga
        {
            var repositoryConfigurator = new DapperSagaRepositoryConfigurator<TSaga>();

            // connection string will only be set from existing code, since it's now Obsolete
            if (!string.IsNullOrEmpty(connectionString))
                repositoryConfigurator.UseSqlServer(connectionString);

            configure?.Invoke(repositoryConfigurator);

            configurator.Repository(repositoryConfigurator.Register);

            return configurator;
        }
    }
}
#nullable restore

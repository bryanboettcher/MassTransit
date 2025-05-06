namespace MassTransit
{
    using System;
    using Configuration;
    
    public static class DapperSagaRepositoryRegistrationExtensions
    {
        public static ISagaRegistrationConfigurator<TSaga> DapperRepository<TSaga>(this ISagaRegistrationConfigurator<TSaga> configurator,
            Action<IDapperSagaRepositoryConfigurator<TSaga>> configure = null)
            where TSaga : class, ISaga
        {
            return DapperRepository(configurator, string.Empty, configure);
        }

        /// <summary>
        /// Adds a Dapper saga repository to the registration
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="connectionString"></param>
        /// <param name="configure"></param>
        /// <typeparam name="TSaga"></typeparam>
        /// <returns></returns>
        public static ISagaRegistrationConfigurator<TSaga> DapperRepository<TSaga>(this ISagaRegistrationConfigurator<TSaga> configurator,
            string connectionString, Action<IDapperSagaRepositoryConfigurator<TSaga>> configure = null)
            where TSaga : class, ISaga
        {
            var repositoryConfigurator = new DapperSagaRepositoryConfigurator<TSaga>();

            // connection string will only be set from existing code, since it's now Obsolete
            if (!string.IsNullOrEmpty(connectionString))
                repositoryConfigurator.UseSqlServer(connectionString);

            configure?.Invoke(repositoryConfigurator);

            repositoryConfigurator.Validate()
                .ThrowIfContainsFailure("The Dapper saga repository configuration is invalid:");

            configurator.Repository(s => repositoryConfigurator.Register(s));

            return configurator;
        }

        /// <summary>
        /// Use the Dapper saga repository for sagas configured by type (without a specific generic call to AddSaga/AddSagaStateMachine)
        /// </summary>
        [Obsolete("Configure connection string via the configurator callback", false)]
        public static void SetDapperSagaRepositoryProvider(this IRegistrationConfigurator configurator, string connectionString,
            Action<IDapperSagaRepositoryConfigurator> configure)
        {
            configurator.SetSagaRepositoryProvider(new DapperSagaRepositoryRegistrationProvider(connectionString, configure));
        }

        /// <summary>
        /// Use the Dapper saga repository for sagas configured by type (without a specific generic call to AddSaga/AddSagaStateMachine)
        /// </summary>
        public static void SetDapperSagaRepositoryProvider(this IRegistrationConfigurator configurator, Action<IDapperSagaRepositoryConfigurator> configure)
        {
            configurator.SetSagaRepositoryProvider(new DapperSagaRepositoryRegistrationProvider(configure));
        }
    }
}

#nullable enable
namespace MassTransit.Configuration
{
    using System;


    public static class DapperJobSagaRepositoryRegistrationExtensions
    {
        /// <summary>
        /// Adds saga repositories for JobConsumers, using the Dapper repository provider.
        /// </summary>
        /// <param name="configurator">The JobConsumer configurator</param>
        /// <param name="configure">The configuration callback</param>
        public static IJobSagaRegistrationConfigurator DapperRepository(
            this IJobSagaRegistrationConfigurator configurator,
            Action<IDapperJobSagaRepositoryConfigurator>? configure = null
        )
        {
            var registrationProvider = new DapperJobSagaRepositoryRegistrationProvider(configure);

            configurator.UseRepositoryRegistrationProvider(registrationProvider);

            return configurator;
        }
    }
}
#nullable restore

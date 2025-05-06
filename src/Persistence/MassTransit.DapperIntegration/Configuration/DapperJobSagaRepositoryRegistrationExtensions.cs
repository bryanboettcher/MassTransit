using System;
using MassTransit.Configuration;

namespace MassTransit
{
    public static class DapperJobSagaRepositoryRegistrationExtensions
    {
        public static IJobSagaRegistrationConfigurator DapperRepository(
            this IJobSagaRegistrationConfigurator configurator,
            Action<IDapperJobSagaRepositoryConfigurator> configure = null
        )
        {
            var registrationProvider = new DapperJobSagaRepositoryRegistrationProvider(configure);

            configurator.UseRepositoryRegistrationProvider(registrationProvider);

            return configurator;
        }
    }
}

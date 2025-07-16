namespace MassTransit.Persistence.Configuration
{
    using Integration.ClaimChecks;
    using Microsoft.Extensions.DependencyInjection;


    public static class BusRegistrationConfiguratorExtensions
    {
        /// <summary>
        /// Adds a simple hosted service that periodically clears out old MessageData entries.
        /// </summary>
        public static void AddCleanupService(this IBusRegistrationConfigurator services, Action<MessageDataCleanupOptions>? configure = null)
        {
            services.AddOptions();
            services.ConfigureOptions<MessageDataCleanupOptionsConfigurator>();

            if (configure is not null)
                services.Configure(configure);

            services.AddHostedService<MessageDataCleanupService>();
        }
    }
}

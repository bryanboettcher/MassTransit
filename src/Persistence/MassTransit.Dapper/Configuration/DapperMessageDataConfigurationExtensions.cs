#nullable enable

namespace MassTransit.Configuration
{
    using System;
    using DapperIntegration.ClaimChecks;

    public static class DapperMessageDataConfigurationExtensions
    {
        /// <summary>
        /// Use an RDBMS for MessageData, with ADO.NET as the driver.  If you really want to.
        /// </summary>
        /// <param name="selector">The MessageData </param>
        /// <param name="configure">Configuration for the MessageData repository</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IMessageDataRepository DapperRepository(this IMessageDataRepositorySelector selector, Action<DapperMessageDataOptions>? configure = null)
        {
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            return new DapperMessageDataRepository();
        }
    }
}
#nullable restore

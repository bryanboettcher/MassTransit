#nullable enable

namespace MassTransit.Configuration
{
    using System;
    using System.Data;
    using System.Data.Common;
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


    public class DapperMessageDataOptions
    {
        public string? ConnectionString { get; set; }
        public string? TableName { get; set; }
        public IsolationLevel? IsolationLevel { get; set; }
        public DatabaseProviders Provider { get; set; }

        // use public fields to prevent binding attempts from the configuration
        public Func<IServiceProvider, IMessageDataSqlFormatter>? SqlFormatterProvider;
        public Func<IServiceProvider, DatabaseContextFactory<TSaga>>? ContextFactoryProvider;
        public Func<IServiceProvider, DbConnection>? DbConnectionProvider;
    }
}
#nullable restore

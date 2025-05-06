#nullable enable
using System;

namespace MassTransit.DapperIntegration.Saga
{
    using System.Data;
    using Configuration;
    using SqlBuilders;


    public class DapperOptions<TSaga>
        where TSaga : class, ISaga
    {
        public string? ConnectionString { get; set; }
        public string? TableName { get; set; }
        public string? IdColumnName { get; set; }
        public IsolationLevel? IsolationLevel { get; set; }
        public DatabaseProviders Provider { get; internal set; }

        public Func<IServiceProvider, ISagaSqlFormatter<TSaga>>? SqlBuilderProvider { get; set; }
        public Func<IServiceProvider, DatabaseContextFactory<TSaga>>? ContextFactoryProvider { get; set; }
        
        [Obsolete("Use ContextFactoryProvider instead", true)]
        public DatabaseContextFactory<TSaga>? ContextFactory { get; set; }
    }
}

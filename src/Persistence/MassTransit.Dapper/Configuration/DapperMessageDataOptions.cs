namespace MassTransit.Configuration;

using System.Data;
using System.Data.Common;


public class DapperMessageDataOptions
{
    public string? ConnectionString { get; set; }
    public string? TableName { get; set; }
    public IsolationLevel? IsolationLevel { get; set; }

    // use public fields to prevent binding attempts from the configuration
    public Func<IServiceProvider, IMessageDataSqlFormatter>? SqlFormatterProvider;
    public Func<IServiceProvider, DatabaseContextFactory<TSaga>>? ContextFactoryProvider;
    public Func<IServiceProvider, DbConnection>? DbConnectionProvider;
}

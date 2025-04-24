#nullable enable
namespace MassTransit.DapperIntegration.Saga
{
    using System.Data;
    using SqlBuilders;


    public class DapperOptions<TSaga>
        where TSaga : class, ISaga
    {
        public string? ConnectionString { get; set; }
        public string? TableName { get; set; }
        public string? IdColumnName { get; set; }
        public IsolationLevel? IsolationLevel { get; set; }
        public DapperDatabaseProvider? Provider { get; set; }
        public SqlBuilder<TSaga>? SqlBuilder { get; set; }
        public DatabaseContextFactory<TSaga>? ContextFactory { get; set; }
    }

    public enum DapperDatabaseProvider
    {
        Default,
        SqlServer,
        Postgres,
    }
}

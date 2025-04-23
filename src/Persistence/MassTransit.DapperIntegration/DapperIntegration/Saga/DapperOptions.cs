#nullable enable
namespace MassTransit.DapperIntegration.Saga
{
    using System;
    using System.Data;
    using SqlBuilders;


    public class DapperOptions<TSaga>
        where TSaga : class, ISaga
    {
        DatabaseContextFactory<TSaga>? _contextFactory;
        
        public string ConnectionString { get; private set; }
        public string TableName { get; private set; }
        public string IdColumnName { get; private set; }
        public IsolationLevel IsolationLevel { get; private set; }
        public DapperDatabaseProvider Provider { get; private set; } = DapperDatabaseProvider.Default;
        public SqlBuilder<TSaga>? SqlBuilder { get; private set; }

        public DatabaseContextFactory<TSaga>? ContextFactory
        {
            get => _contextFactory ?? BuildContextFactory();
            private set => _contextFactory = value;
        }
        
        public void UseSqlServer(string connectionString)
        {
            ConnectionString = connectionString;
            Provider = DapperDatabaseProvider.SqlServer;
        }

        public void UsePostgres(string connectionString)
        {
            ConnectionString = connectionString;
            Provider = DapperDatabaseProvider.Postgres;
        }

        public void UseIsolationLevel(IsolationLevel isolationLevel)
            => IsolationLevel = isolationLevel;

        public void UseTableName(string tableName)
            => TableName = tableName;

        public void UseIdColumnName(string idColumnName)
            => IdColumnName = idColumnName;

        public void UseSqlBuilder(SqlBuilder<TSaga> sqlBuilder)
            => SqlBuilder = sqlBuilder;

        public void UseContextFactory(DatabaseContextFactory<TSaga> contextFactory)
            => ContextFactory = contextFactory;

        DatabaseContextFactory<TSaga> BuildContextFactory()
        {
            var sqlBuilder = SqlBuilder ?? Provider switch
            {
                DapperDatabaseProvider.Default => null,
                DapperDatabaseProvider.SqlServer => new SqlServerBuilder<TSaga>(TableName, IdColumnName),
                DapperDatabaseProvider.Postgres => new PostgresBuilder<TSaga>(TableName, IdColumnName),
                _ => throw new ArgumentOutOfRangeException(nameof(Provider))
            };

            return Provider switch
            {
                DapperDatabaseProvider.Default => (c, t) => new DapperDatabaseContext<TSaga>(c, t),
                DapperDatabaseProvider.SqlServer => (c, t) => new SagaDatabaseContext<TSaga>(c, t, sqlBuilder),
                DapperDatabaseProvider.Postgres => (c, t) => new SagaDatabaseContext<TSaga>(c, t, sqlBuilder),
                _ => throw new ArgumentOutOfRangeException(nameof(Provider))
            };
        }
    }

    public enum DapperDatabaseProvider
    {
        Default,
        SqlServer,
        Postgres,
    }
}

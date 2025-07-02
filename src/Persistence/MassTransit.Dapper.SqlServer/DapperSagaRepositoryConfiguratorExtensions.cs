namespace MassTransit.Dapper.SqlServer
{
    using System.Data;
    using Configuration;
    using DapperIntegration.Saga;
    using DapperIntegration.SqlBuilders;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.DependencyInjection;


    public static class DapperSagaRepositoryConfiguratorExtensions
    {
        public static IDapperRepositoryConfigurator<TSaga> UsingSqlServer<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> dapperConfigurator,
            Action<ISqlServerRepositoryConfigurator<TSaga>> configure) where TSaga : class, ISaga
        {
            var connectionConfiguration = new SqlServerRepositoryConfigurator<TSaga>(dapperConfigurator);

            configure.Invoke(connectionConfiguration);

            return dapperConfigurator;
        }

        public static IDapperRepositoryConfigurator<TSaga> UsingSqlServer<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> dapperConfigurator,
            string hostname, string catalog, string username, string password) where TSaga : class, ISaga
        {
            var connectionConfiguration = new SqlServerRepositoryConfigurator<TSaga>(dapperConfigurator);
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = hostname,
                InitialCatalog = catalog,
                UserID = username,
                Password = password
            };

            connectionConfiguration.SetConnectionString(connectionStringBuilder.ToString());
            return dapperConfigurator;
        }

        public static IDapperRepositoryConfigurator<TSaga> UsingSqlServer<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> dapperConfigurator,
            string connectionString) where TSaga : class, ISaga
        {
            var connectionConfiguration = new SqlServerRepositoryConfigurator<TSaga>(dapperConfigurator);
            
            connectionConfiguration.SetConnectionString(connectionString);
            return dapperConfigurator;
        }
    }
    
    public interface ISqlServerRepositoryConfigurator<TSaga>
        where TSaga : class, ISaga
    {
        ISqlServerRepositoryConfigurator<TSaga> SetConnectionString(string connectionString);
        ISqlServerRepositoryConfigurator<TSaga> SetIsolationLevel(IsolationLevel isolationLevel);
        ISqlServerRepositoryConfigurator<TSaga> SetConcurrencyMode(ConcurrencyMode concurrencyMode);

        string? ConnectionString { get; set; }
        IsolationLevel IsolationLevel { get; set; }
        ConcurrencyMode ConcurrencyMode { get; set; }
    }

    public class SqlServerRepositoryConfigurator<TSaga> : ISqlServerRepositoryConfigurator<TSaga>
        where TSaga : class, ISaga
    {
        readonly IDapperRepositoryConfigurator<TSaga> _dapperConfigurator;
        
        public SqlServerRepositoryConfigurator(IDapperRepositoryConfigurator<TSaga> dapperConfigurator)
            => _dapperConfigurator = dapperConfigurator;

        public void Configure()
        {
            _dapperConfigurator.SetSqlFormatter(null);
            _dapperConfigurator.SetConnectionProvider(null);
            _dapperConfigurator.SetContextFactory(ConfiguredSqlServerContextFactory);
        }

        async Task<DatabaseContext<TSaga>> ConfiguredSqlServerContextFactory(IServiceProvider serviceProvider)
        {
            var formatter = serviceProvider.GetRequiredService<ISagaSqlFormatter<TSaga>>();
            var provider = serviceProvider.GetRequiredService<ISagaConnectionProvider<TSaga>>();

            
        }

        public string? ConnectionString { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public ConcurrencyMode ConcurrencyMode { get; set; }

        public ISqlServerRepositoryConfigurator<TSaga> SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
            return this;
        }

        public ISqlServerRepositoryConfigurator<TSaga> SetIsolationLevel(IsolationLevel isolationLevel)
        {
            IsolationLevel = isolationLevel;
            return this;
        }

        public ISqlServerRepositoryConfigurator<TSaga> SetConcurrencyMode(ConcurrencyMode concurrencyMode)
        {
            ConcurrencyMode = concurrencyMode;
            return this;
        }
    }
}

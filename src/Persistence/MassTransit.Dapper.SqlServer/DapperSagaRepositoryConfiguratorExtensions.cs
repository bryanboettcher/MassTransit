namespace MassTransit.Dapper.SqlServer
{
    public static class DapperSagaRepositoryConfiguratorExtensions
    {
        public static IDapperSqlServerRepositoryConfigurator<TSaga> UsingSqlServer<TSaga>(
            this IDapperRepositoryConfigurator<TSaga> sagaConfigurator,
            Action<ISqlServerRepositoryConfigurator<TSaga>>? configure = null) where TSaga : class, ISaga
        {
            var configurator = new SqlServerRepositoryConfigurator<TSaga>();
            configure?.Invoke(configurator);

            return sagaConfigurator;
        }
    }


    public interface IDapperSqlServerRepositoryConfigurator<TSaga> : IDapperRepositoryConfigurator<TSaga>
    {
    }

    public interface ISqlServerRepositoryConfigurator<TSaga>
        where TSaga : class, ISaga
    {}

    public class SqlServerRepositoryConfigurator<TSaga> : ISqlServerRepositoryConfigurator<TSaga>
        where TSaga : class, ISaga
    { }
}

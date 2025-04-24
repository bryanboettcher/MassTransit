#nullable enable
namespace MassTransit;

using System.Data;
using DapperIntegration.Saga;
using DapperIntegration.SqlBuilders;


public interface IDapperSagaRepositoryConfigurator
{
    IsolationLevel? IsolationLevel { set; }
    string? ConnectionString { get; set; }
    string? TableName { get; set; }
    string? IdColumnName { get; set; }
    DapperDatabaseProvider? Provider { get; set; }

    IDapperSagaRepositoryConfigurator UseSqlServer(string connectionString);
    IDapperSagaRepositoryConfigurator UsePostgres(string connectionString);
    IDapperSagaRepositoryConfigurator UseIsolationLevel(IsolationLevel isolationLevel);
    IDapperSagaRepositoryConfigurator UseTableName(string tableName);
    IDapperSagaRepositoryConfigurator UseIdColumnName(string idColumnName);
    IDapperSagaRepositoryConfigurator UseProvider(DapperDatabaseProvider provider);

}


public interface IDapperSagaRepositoryConfigurator<TSaga> :
    IDapperSagaRepositoryConfigurator
    where TSaga : class, ISaga
{
    /// <summary>
    /// Set the database context factory to allow customization of the Dapper interaction/queries
    /// </summary>
    DatabaseContextFactory<TSaga> ContextFactory { set; }
    SqlBuilder<TSaga>? SqlBuilder { get; set; }
    
    IDapperSagaRepositoryConfigurator<TSaga> UseSqlBuilder(SqlBuilder<TSaga> sqlBuilder);
    IDapperSagaRepositoryConfigurator<TSaga> UseContextFactory(DatabaseContextFactory<TSaga> contextFactory);
}
#nullable restore

namespace MassTransit.Persistence.Tests.IntegrationTests.Connectors;

using Configuration;
using Dapper;
using MassTransit.Tests;
using Microsoft.Data.SqlClient;
using SqlServer.Configuration;
using StateMachineSagas;


public class OptimisticSqlServerConnector : SqlServerConnector, TestConnector
{
    public byte[] RowVersion { get; set; }
    
    void TestConnector.Connect<TSaga>(IAdoRepositoryConfigurator<TSaga> conf)
        => conf.UsingSqlServer(ConnectionString, opt => opt.SetTableName("OptimisticSagas").SetOptimisticConcurrency());

    public Task<List<TSaga>> GetSagas<TSaga>() where TSaga : class, ISaga
        => base.GetSagas<TSaga>("OptimisticSagas");
}

public class PessimisticSqlServerConnector : SqlServerConnector, TestConnector
{
    void TestConnector.Connect<TSaga>(IAdoRepositoryConfigurator<TSaga> conf)
        => conf.UsingSqlServer(ConnectionString, opt => opt.SetTableName("PessimisticSagas").SetPessimisticConcurrency());

    public Task<List<TSaga>> GetSagas<TSaga>() where TSaga : class, ISaga
        => base.GetSagas<TSaga>("PessimisticSagas");
}

public abstract class SqlServerConnector : BehaviorSaga
{
    protected readonly string ConnectionString;

    public SqlServerConnector()
    {
        ConnectionString = LocalDbConnectionStringProvider.GetLocalDbConnectionString();
    }

    public async Task Setup()
    {
        await RunSql(Sql.SqlServer_DropJobTables);
        await RunSql(Sql.SqlServer_DropSagaTables);

        await RunSql(Sql.SqlServer_CreateJobTables);
        await RunSql(Sql.SqlServer_CreateSagaTables);
    }

    public async Task Reset()
    {
        await RunSql(Sql.SqlServer_ResetJobTables);
        await RunSql(Sql.SqlServer_ResetSagaTables);
    }

    public async Task Teardown()
    {
        await RunSql(Sql.SqlServer_DropJobTables);
        await RunSql(Sql.SqlServer_DropSagaTables);
    }

    public void Connect(IAdoJobSagaRepositoryConfigurator conf)
        => conf.UsingSqlServer(ConnectionString);

    protected async Task<List<TSaga>> GetSagas<TSaga>(string tableName)
    {
        await using var connection = new SqlConnection(ConnectionString);

        var sql = $"SELECT * FROM {tableName};";
        return (await connection.QueryAsync<TSaga>(sql)).AsList();
    }

    protected async Task RunSql(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync();
    }
}

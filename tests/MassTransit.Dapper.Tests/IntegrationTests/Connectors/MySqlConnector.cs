namespace MassTransit.Dapper.Tests.IntegrationTests.Connectors;

using Configuration;
using global::Dapper;
using global::MySql.Data.MySqlClient;
using MySql.Configuration;
using StateMachineSagas;


public class OptimisticMySqlConnector : MySqlConnector, TestConnector
{
    public DateTime RowVersion { get; set; }

    public void Connect<TSaga>(IAdoRepositoryConfigurator<TSaga> conf)
        where TSaga : class, ISaga
    {
        conf.UsingMySql(ConnectionString, opt => opt.SetTableName("OptimisticSagas").SetOptimisticConcurrency());
    }

    public Task<List<TSaga>> GetSagas<TSaga>()
        where TSaga : class, ISaga =>
        base.GetSagas<TSaga>("OptimisticSagas");
}

public class PessimisticMySqlConnector : MySqlConnector, TestConnector
{
    public void Connect<TSaga>(IAdoRepositoryConfigurator<TSaga> conf)
        where TSaga : class, ISaga
    {
        conf.UsingMySql(ConnectionString, opt => opt.SetTableName("PessimisticSagas").SetPessimisticConcurrency());
    }

    public Task<List<TSaga>> GetSagas<TSaga>()
        where TSaga : class, ISaga =>
        base.GetSagas<TSaga>("PessimisticSagas");
}

public abstract class MySqlConnector : BehaviorSaga
{
    protected readonly string ConnectionString;

    public MySqlConnector()
    {
        ConnectionString = "Server=localhost; Database=masstransit; Uid=root; Pwd=Password12!; OldGuids=true";
    }

    public async Task Setup()
    {
        await RunSql(Sql.MySql_DropJobTables);
        await RunSql(Sql.MySql_DropSagaTables);

        await RunSql(Sql.MySql_CreateJobTables);
        await RunSql(Sql.MySql_CreateSagaTables);
    }

    public async Task Reset()
    {
        await RunSql(Sql.MySql_ResetJobTables);
        await RunSql(Sql.MySql_ResetSagaTables);
    }

    public async Task Teardown()
    {
        await RunSql(Sql.MySql_DropJobTables);
        await RunSql(Sql.MySql_DropSagaTables);
    }

    public void Connect(IAdoJobSagaRepositoryConfigurator conf)
        => conf.UsingMySql(ConnectionString);

    protected async Task<List<TSaga>> GetSagas<TSaga>(string tableName)
    {
        await using var connection = new MySqlConnection(ConnectionString);

        var sql = $"SELECT * FROM {tableName};";
        return (await connection.QueryAsync<TSaga>(sql)).AsList();
    }

    async Task RunSql(string sql)
    {
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync();
    }
}

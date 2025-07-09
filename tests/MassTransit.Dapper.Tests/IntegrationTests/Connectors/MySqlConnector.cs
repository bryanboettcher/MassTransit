namespace MassTransit.Dapper.Tests.IntegrationTests.Connectors;

using Configuration;
using global::Dapper;
using StateMachineSagas;
using MySqlql.Configuration;
using Npgsql;


public class OptimisticMySqlConnector : MySqlConnector, TestConnector
{
    public uint XMin { get; set; }

    public void Connect<TSaga>(IAdoRepositoryConfigurator<TSaga> conf)
        where TSaga : class, ISaga
    {
        conf.UsingMySql(ConnectionString, opt => opt.SetOptimisticConcurrency());
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
        conf.UsingMySql(ConnectionString, opt => opt.SetPessimisticConcurrency());
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
        ConnectionString = "Server=localhost; Database=masstransit; Uid=root; Pwd=Password12!";
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
        await using var connection = new NpgsqlConnection(ConnectionString);

        var sql = $"SELECT * FROM {tableName};";
        return (await connection.QueryAsync<TSaga>(sql)).AsList();
    }

    async Task RunSql(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync();
    }
}

namespace MassTransit.Dapper.Tests.IntegrationTests.Connectors;

using Configuration;
using global::Dapper;
using StateMachineSagas;
using Npgsql;
using PostgreSql.Configuration;


public class OptimisticPostgresConnector : PostgresConnector, TestConnector
{
    public uint XMin { get; set; }

    public void Connect<TSaga>(IAdoRepositoryConfigurator<TSaga> conf)
        where TSaga : class, ISaga
    {
        conf.UsingPostgres(ConnectionString, opt => opt.SetOptimisticConcurrency());
    }

    public Task<List<TSaga>> GetSagas<TSaga>()
        where TSaga : class, ISaga =>
        base.GetSagas<TSaga>("OptimisticSagas");
}

public class PessimisticPostgresConnector : PostgresConnector, TestConnector
{
    public void Connect<TSaga>(IAdoRepositoryConfigurator<TSaga> conf)
        where TSaga : class, ISaga
    {
        conf.UsingPostgres(ConnectionString, opt => opt.SetPessimisticConcurrency());
    }

    public Task<List<TSaga>> GetSagas<TSaga>()
        where TSaga : class, ISaga =>
        base.GetSagas<TSaga>("PessimisticSagas");
}

public abstract class PostgresConnector : BehaviorSaga
{
    protected readonly string ConnectionString;
    
    public PostgresConnector()
    {
        ConnectionString = "Host=localhost; Username=postgres; Password=Password12!; Database=masstransit";
    }

    public async Task Setup()
    {
        await RunSql(Sql.Postgres_DropJobTables);
        await RunSql(Sql.Postgres_DropSagaTables);

        await RunSql(Sql.Postgres_CreateJobTables);
        await RunSql(Sql.Postgres_CreateSagaTables);
    }

    public async Task Reset()
    {
        await RunSql(Sql.Postgres_ResetJobTables);
        await RunSql(Sql.Postgres_ResetSagaTables);
    }

    public async Task Teardown()
    {
        await RunSql(Sql.Postgres_DropJobTables);
        await RunSql(Sql.Postgres_DropSagaTables);
    }

    public void Connect(IAdoJobSagaRepositoryConfigurator conf)
        => conf.UsingPostgres(ConnectionString);

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

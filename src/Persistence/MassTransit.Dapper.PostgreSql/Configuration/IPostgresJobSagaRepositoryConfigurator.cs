namespace MassTransit.Dapper.PostgreSql.Configuration;

public interface IPostgresJobSagaRepositoryConfigurator
{
    IPostgresJobSagaRepositoryConfigurator SetConnectionString(string connectionString);
    
    string ConnectionString { get; set; }
}

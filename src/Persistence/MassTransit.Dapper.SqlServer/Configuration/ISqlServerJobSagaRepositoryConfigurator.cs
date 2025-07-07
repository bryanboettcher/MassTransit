namespace MassTransit.Dapper.SqlServer.Configuration;

public interface ISqlServerJobSagaRepositoryConfigurator
{
    ISqlServerJobSagaRepositoryConfigurator SetConnectionString(string connectionString);
    
    string ConnectionString { get; set; }
}

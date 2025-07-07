namespace MassTransit.Dapper.SqlServer.Configuration;

using Dapper.Configuration;
using DapperIntegration.JobSagas;
using DapperIntegration.Saga;
using Microsoft.Extensions.DependencyInjection;


public class SqlServerJobSagaRepositoryConfigurator : ISqlServerJobSagaRepositoryConfigurator, ISpecification
{
    public string? ConnectionString { get; set; }
    
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure("ConnectionString must be specified");
    }

    public void Configure(IDapperJobSagaRepositoryConfigurator configurator)
    {
        configurator.SetJobContextFactory(isp => Task.FromResult<DatabaseContext<JobSaga>>(isp.GetRequiredService<JobSagaDatabaseContext>()));
        configurator.SetJobTypeContextFactory(isp => Task.FromResult<DatabaseContext<JobTypeSaga>>(isp.GetRequiredService<JobTypeSagaDatabaseContext>()));
        configurator.SetJobAttemptContextFactory(isp => Task.FromResult<DatabaseContext<JobAttemptSaga>>(isp.GetRequiredService<JobAttemptSagaDatabaseContext>()));
    }

    public ISqlServerJobSagaRepositoryConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }
}

namespace MassTransit.Persistence.SqlServer.Configuration;

using System.Data;
using Connections;
using Integration.JobSagas;
using Integration.Saga;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Configuration;


public class SqlServerJobSagaRepositoryConfigurator : ISqlServerJobSagaRepositoryConfigurator, ISpecification
{
    /// <inheritdoc />
    public string? ConnectionString { get; set; }

    /// <inheritdoc />
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;

    /// <inheritdoc />
    public ISqlServerJobSagaRepositoryConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    /// <inheritdoc />
    public ISqlServerJobSagaRepositoryConfigurator SetIsolationLevel(IsolationLevel isolationLevel)
    {
        IsolationLevel = isolationLevel;
        return this;
    }

    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure("ConnectionString must be specified");
    }

    public void Configure(IAdoJobSagaRepositoryConfigurator configurator)
    {
        (configurator as AdoJobSagaRepositoryConfigurator)?.AddCallback(RegisterDependencies);

        configurator.SetJobContextFactory(sp => Task.FromResult<DatabaseContext<JobSaga>>(sp.GetRequiredService<JobSagaDatabaseContext>()));
        configurator.SetJobTypeContextFactory(sp => Task.FromResult<DatabaseContext<JobTypeSaga>>(sp.GetRequiredService<JobTypeSagaDatabaseContext>()));
        configurator.SetJobAttemptContextFactory(sp => Task.FromResult<DatabaseContext<JobAttemptSaga>>(sp.GetRequiredService<JobAttemptSagaDatabaseContext>()));
    }

    void RegisterDependencies(IServiceCollection services)
    {
        ArgumentException.ThrowIfNullOrEmpty(ConnectionString);

        services.AddScoped<SagaSerializer<JobSaga, JobSagaDatabaseContext.DbModel>, JobSagaDatabaseContext.Serializer>();
        services.AddScoped<DatabaseContext<JobSagaDatabaseContext.DbModel>>(
            _ => new PessimisticSqlServerDatabaseContext<JobSagaDatabaseContext.DbModel>(ConnectionString, "Jobs", nameof(ISaga.CorrelationId), IsolationLevel)
        );

        services.AddScoped<SagaSerializer<JobTypeSaga, JobTypeSagaDatabaseContext.DbModel>, JobTypeSagaDatabaseContext.Serializer>();
        services.AddScoped<DatabaseContext<JobTypeSagaDatabaseContext.DbModel>>(
            _ => new PessimisticSqlServerDatabaseContext<JobTypeSagaDatabaseContext.DbModel>(ConnectionString, "JobTypes", nameof(ISaga.CorrelationId), IsolationLevel)
        );

        services.AddScoped<SagaSerializer<JobAttemptSaga, JobAttemptSagaDatabaseContext.DbModel>, JobAttemptSagaDatabaseContext.Serializer>();
        services.AddScoped<DatabaseContext<JobAttemptSagaDatabaseContext.DbModel>>(
            _ => new PessimisticSqlServerDatabaseContext<JobAttemptSagaDatabaseContext.DbModel>(ConnectionString, "JobAttempts", nameof(ISaga.CorrelationId), IsolationLevel)
        );
    }
}

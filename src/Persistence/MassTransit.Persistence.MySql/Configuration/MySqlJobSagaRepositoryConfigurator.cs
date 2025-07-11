namespace MassTransit.Persistence.MySql.Configuration;

using System.Data;
using Integration.JobSagas;
using Integration.Saga;
using MassTransit.Persistence.MySql.Connections;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Configuration;


public class MySqlJobSagaRepositoryConfigurator : IMySqlJobSagaRepositoryConfigurator, ISpecification
{
    /// <inheritdoc />
    public string? ConnectionString { get; set; }

    /// <inheritdoc />
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;

    /// <inheritdoc />
    public IMySqlJobSagaRepositoryConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    /// <inheritdoc />
    public IMySqlJobSagaRepositoryConfigurator SetIsolationLevel(IsolationLevel isolationLevel)
    {
        IsolationLevel = isolationLevel;
        return this;
    }

    /// <inheritdoc />
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
            _ => new PessimisticMySqlDatabaseContext<JobSagaDatabaseContext.DbModel>(ConnectionString, "Jobs", nameof(ISaga.CorrelationId), IsolationLevel)
        );

        services.AddScoped<SagaSerializer<JobTypeSaga, JobTypeSagaDatabaseContext.DbModel>, JobTypeSagaDatabaseContext.Serializer>();
        services.AddScoped<DatabaseContext<JobTypeSagaDatabaseContext.DbModel>>(
            _ => new PessimisticMySqlDatabaseContext<JobTypeSagaDatabaseContext.DbModel>(ConnectionString, "JobTypes", nameof(ISaga.CorrelationId), IsolationLevel)
        );

        services.AddScoped<SagaSerializer<JobAttemptSaga, JobAttemptSagaDatabaseContext.DbModel>, JobAttemptSagaDatabaseContext.Serializer>();
        services.AddScoped<DatabaseContext<JobAttemptSagaDatabaseContext.DbModel>>(
            _ => new PessimisticMySqlDatabaseContext<JobAttemptSagaDatabaseContext.DbModel>(ConnectionString, "JobAttempts", nameof(ISaga.CorrelationId), IsolationLevel)
        );
    }
}

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

        configurator.SetJobContextFactory(sp => Task.FromResult(sp.GetRequiredService<DatabaseContext<JobSaga>>()));
        configurator.SetJobTypeContextFactory(sp => Task.FromResult(sp.GetRequiredService<DatabaseContext<JobTypeSaga>>()));
        configurator.SetJobAttemptContextFactory(sp => Task.FromResult(sp.GetRequiredService<DatabaseContext<JobAttemptSaga>>()));
    }

    void RegisterDependencies(IServiceCollection services)
    {
        ArgumentException.ThrowIfNullOrEmpty(ConnectionString);

        services.AddTransient<DatabaseContext<JobSaga>, JobSagaDatabaseContext>();
        services.AddTransient<SagaSerializer<JobSaga, JobSagaDatabaseContext.DbModel>, JobSagaDatabaseContext.Serializer>();
        services.AddTransient<DatabaseContext<JobSagaDatabaseContext.DbModel>>(
            _ => new PessimisticMySqlDatabaseContext<JobSagaDatabaseContext.DbModel>(ConnectionString, "Jobs", nameof(ISaga.CorrelationId), IsolationLevel)
        );

        services.AddTransient<DatabaseContext<JobTypeSaga>, JobTypeSagaDatabaseContext>();
        services.AddTransient<SagaSerializer<JobTypeSaga, JobTypeSagaDatabaseContext.DbModel>, JobTypeSagaDatabaseContext.Serializer>();
        services.AddTransient<DatabaseContext<JobTypeSagaDatabaseContext.DbModel>>(
            _ => new PessimisticMySqlDatabaseContext<JobTypeSagaDatabaseContext.DbModel>(ConnectionString, "JobTypes", nameof(ISaga.CorrelationId), IsolationLevel)
        );

        services.AddTransient<DatabaseContext<JobAttemptSaga>, JobAttemptSagaDatabaseContext>();
        services.AddTransient<SagaSerializer<JobAttemptSaga, JobAttemptSagaDatabaseContext.DbModel>, JobAttemptSagaDatabaseContext.Serializer>();
        services.AddTransient<DatabaseContext<JobAttemptSagaDatabaseContext.DbModel>>(
            _ => new PessimisticMySqlDatabaseContext<JobAttemptSagaDatabaseContext.DbModel>(ConnectionString, "JobAttempts", nameof(ISaga.CorrelationId), IsolationLevel)
        );
    }
}

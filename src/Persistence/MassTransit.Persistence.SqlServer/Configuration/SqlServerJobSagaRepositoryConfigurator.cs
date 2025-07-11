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
            _ => new PessimisticSqlServerDatabaseContext<JobSagaDatabaseContext.DbModel>(ConnectionString, "Jobs", nameof(ISaga.CorrelationId), IsolationLevel)
        );

        services.AddTransient<DatabaseContext<JobTypeSaga>, JobTypeSagaDatabaseContext>();
        services.AddTransient<SagaSerializer<JobTypeSaga, JobTypeSagaDatabaseContext.DbModel>, JobTypeSagaDatabaseContext.Serializer>();
        services.AddTransient<DatabaseContext<JobTypeSagaDatabaseContext.DbModel>>(
            _ => new PessimisticSqlServerDatabaseContext<JobTypeSagaDatabaseContext.DbModel>(ConnectionString, "JobTypes", nameof(ISaga.CorrelationId), IsolationLevel)
        );

        services.AddTransient<DatabaseContext<JobAttemptSaga>, JobAttemptSagaDatabaseContext>();
        services.AddTransient<SagaSerializer<JobAttemptSaga, JobAttemptSagaDatabaseContext.DbModel>, JobAttemptSagaDatabaseContext.Serializer>();
        services.AddTransient<DatabaseContext<JobAttemptSagaDatabaseContext.DbModel>>(
            _ => new PessimisticSqlServerDatabaseContext<JobAttemptSagaDatabaseContext.DbModel>(ConnectionString, "JobAttempts", nameof(ISaga.CorrelationId), IsolationLevel)
        );
    }
}

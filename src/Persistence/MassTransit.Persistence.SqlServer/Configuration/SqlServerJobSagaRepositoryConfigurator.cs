namespace MassTransit.Persistence.SqlServer.Configuration;

using System.Data;
using Components.JobConsumers;
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

        services.AddTransient<DatabaseContext<JobSaga>>(_
            => new JobSagaDatabaseContext(ConnectionString, IsolationLevel)
        );

        services.AddTransient<DatabaseContext<JobTypeSaga>>(_
            => new JobTypeSagaDatabaseContext(ConnectionString, IsolationLevel)
        );

        services.AddTransient<DatabaseContext<JobAttemptSaga>>(_
            => new JobAttemptSagaDatabaseContext(ConnectionString, IsolationLevel)
        );
    }
}

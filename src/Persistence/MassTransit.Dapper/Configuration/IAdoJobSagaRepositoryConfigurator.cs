namespace MassTransit.Dapper.Configuration;

/// <summary>
/// Enables JobConsumer support via preconfigured saga repositories.
/// </summary>
public interface IAdoJobSagaRepositoryConfigurator
{
    /// <summary>
    /// Set a custom context factory for JobSagas.
    /// </summary>
    IAdoJobSagaRepositoryConfigurator SetJobContextFactory(DatabaseContextFactory<JobSaga> contextFactory);

    /// <summary>
    /// Set a custom context factory for JobTypeSagas.
    /// </summary>
    IAdoJobSagaRepositoryConfigurator SetJobTypeContextFactory(DatabaseContextFactory<JobTypeSaga> contextFactory);

    /// <summary>
    /// Set a custom context factory for JobAttemptSagas.
    /// </summary>
    IAdoJobSagaRepositoryConfigurator SetJobAttemptContextFactory(DatabaseContextFactory<JobAttemptSaga> contextFactory);

    /// <summary>
    /// Gets/sets a custom context factory for JobSagas.
    /// </summary>
    DatabaseContextFactory<JobSaga> JobContextFactory { get; set; }

    /// <summary>
    /// Gets/sets a custom context factory for JobTypeSagas.
    /// </summary>
    DatabaseContextFactory<JobTypeSaga> JobTypeContextFactory { get; set; }

    /// <summary>
    /// Gets/sets a custom context factory for JobAttemptSagas.
    /// </summary>
    DatabaseContextFactory<JobAttemptSaga> JobAttemptContextFactory { get; set; }
}

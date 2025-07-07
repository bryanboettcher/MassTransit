namespace MassTransit.Dapper.Configuration;

public interface IDapperJobSagaRepositoryConfigurator
{
    IDapperJobSagaRepositoryConfigurator SetJobContextFactory(DatabaseContextFactory<JobSaga> contextFactory);
    IDapperJobSagaRepositoryConfigurator SetJobTypeContextFactory(DatabaseContextFactory<JobTypeSaga> contextFactory);
    IDapperJobSagaRepositoryConfigurator SetJobAttemptContextFactory(DatabaseContextFactory<JobAttemptSaga> contextFactory);

    DatabaseContextFactory<JobSaga> JobContextFactory { get; set; }
    DatabaseContextFactory<JobTypeSaga> JobTypeContextFactory { get; set; }
    DatabaseContextFactory<JobAttemptSaga> JobAttemptContextFactory { get; set; }
}
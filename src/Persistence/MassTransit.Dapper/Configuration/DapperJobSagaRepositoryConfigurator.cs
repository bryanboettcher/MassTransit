namespace MassTransit.Dapper.Configuration;

using DependencyInjection.Registration;
using MassTransit.Configuration;
using Microsoft.Extensions.DependencyInjection;


public class DapperJobSagaRepositoryConfigurator : IDapperJobSagaRepositoryConfigurator, ISpecification,
    ISagaRepositoryRegistrationProvider
{
    readonly List<Action<IServiceCollection>> _callbacks = new();

    public IEnumerable<ValidationResult> Validate()
    {
        if (JobContextFactory is null)
            yield return this.Failure($"{nameof(JobContextFactory)} must be set");

        if (JobTypeContextFactory is null)
            yield return this.Failure($"{nameof(JobTypeContextFactory)} must be set");

        if (JobAttemptContextFactory is null)
            yield return this.Failure($"{nameof(JobAttemptContextFactory)} must be set");
    }

    public void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        configurator.Repository(services => _callbacks.ForEach(c => c.Invoke(services)));
        _callbacks.Clear();

        switch ( configurator )
        {
            case SagaRegistrationConfigurator<JobSaga> job:
                job.Repository(services => Register(
                    JobContextFactory!,
                    services));
                break;

            case SagaRegistrationConfigurator<JobTypeSaga> jobType:
                jobType.Repository(services => Register(
                    JobTypeContextFactory!,
                    services));
                break;

            case SagaRegistrationConfigurator<JobAttemptSaga> jobAttempt:
                jobAttempt.Repository(services => Register(
                    JobAttemptContextFactory!,
                    services));
                break;
        }
        
        return;

        static void Register<T>(DatabaseContextFactory<T> contextFactory, ISagaRepositoryRegistrationConfigurator<T> services)
            where T : class, ISaga
        {
            var cfg = new DapperRepositoryConfigurator<T>();
            cfg.SetContextFactory(contextFactory);
            cfg.Register(services);
        }
    }

    public IDapperJobSagaRepositoryConfigurator SetJobContextFactory(DatabaseContextFactory<JobSaga> contextFactory)
    {
        JobContextFactory = contextFactory;
        return this;
    }

    public IDapperJobSagaRepositoryConfigurator SetJobTypeContextFactory(DatabaseContextFactory<JobTypeSaga> contextFactory)
    {
        JobTypeContextFactory = contextFactory;
        return this;
    }

    public IDapperJobSagaRepositoryConfigurator SetJobAttemptContextFactory(DatabaseContextFactory<JobAttemptSaga> contextFactory)
    {
        JobAttemptContextFactory = contextFactory;
        return this;
    }

    public DatabaseContextFactory<JobSaga>? JobContextFactory { get; set; }

    public DatabaseContextFactory<JobTypeSaga>? JobTypeContextFactory { get; set; }

    public DatabaseContextFactory<JobAttemptSaga>? JobAttemptContextFactory { get; set; }

    public void AddCallback(Action<IServiceCollection> callback)
        => _callbacks.Add(callback);
}
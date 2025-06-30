namespace MassTransit.Configuration;

using Internals;


public class DapperJobSagaRepositoryRegistrationProvider :
    ISagaRepositoryRegistrationProvider
{
    readonly Action<IDapperJobSagaRepositoryConfigurator>? _configure;

    public DapperJobSagaRepositoryRegistrationProvider(Action<IDapperJobSagaRepositoryConfigurator>? configure)
    {
        _configure = configure;
    }

    public virtual void Configure<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga
    {
        var jobConfigurator = new DapperJobSagaRepositoryConfigurator();
        _configure?.Invoke(jobConfigurator);

        switch (configurator)
        {
            case ISagaRegistrationConfigurator<JobSaga> jobConfig:
                jobConfig.Repository(jobConfigurator.RegisterJob);
                break;

            case ISagaRegistrationConfigurator<JobTypeSaga> jobTypeConfig:
                jobTypeConfig.Repository(jobConfigurator.RegisterJobType);
                break;

            case ISagaRegistrationConfigurator<JobAttemptSaga> jobAttemptConfig:
                jobAttemptConfig.Repository(jobConfigurator.RegisterJobAttempt);
                break;

            default:
                throw new InvalidOperationException($"Unexpected configurator type: {configurator.GetType().GetTypeName()}");
        }
    }
}

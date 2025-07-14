namespace MassTransit.Persistence.Integration.JobSagas
{
    using Saga;
    
    public class JobAttemptSagaDatabaseContext : DatabaseContext<JobAttemptSaga>
    {
    }
    
    public class JobSagaDatabaseContext : DatabaseContext<JobSaga>
    {
    }
    
    public class JobTypeSagaDatabaseContext : DatabaseContext<JobTypeSaga>
    {
    }
}

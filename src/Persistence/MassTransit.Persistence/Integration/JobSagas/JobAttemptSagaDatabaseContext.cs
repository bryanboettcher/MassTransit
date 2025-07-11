namespace MassTransit.Persistence.Integration.JobSagas
{
    using Saga;


    /// <summary>
    /// An implementation of the JobAttempt saga repository.
    /// </summary>
    public class JobAttemptSagaDatabaseContext : JobSagaBaseContext<JobAttemptSaga, JobAttemptSagaDatabaseContext.DbModel>, DatabaseContext<JobAttemptSaga>
    {
        public JobAttemptSagaDatabaseContext(
            DatabaseContext<DbModel> databaseContext,
            SagaSerializer<JobAttemptSaga, DbModel> serializer
        ) : base(databaseContext, serializer)
        { }

        public class Serializer : SystemTextJsonSagaSerializerBase<JobAttemptSaga, DbModel>
        {
            public override DbModel FromSaga(JobAttemptSaga instance)
            {
                var model = new DbModel
                {
                    CorrelationId = instance.CorrelationId,
                    CurrentState = instance.CurrentState,
                    JobId = instance.JobId,
                    Started = instance.Started,
                    Faulted = instance.Faulted,
                    StatusCheckTokenId = instance.StatusCheckTokenId,
                    RetryAttempt = instance.RetryAttempt,
                    ServiceAddress = instance.ServiceAddress?.ToString(),
                    InstanceAddress = instance.InstanceAddress?.ToString(),
                };

                return model;
            }

            public override JobAttemptSaga? FromModel(DbModel? model)
            {
                if (model is null)
                    return null;

                var instance = new JobAttemptSaga
                {
                    CorrelationId = model.CorrelationId,
                    CurrentState = model.CurrentState,
                    JobId = model.JobId,
                    Started = model.Started,
                    Faulted = model.Faulted,
                    StatusCheckTokenId = model.StatusCheckTokenId,
                    RetryAttempt = model.RetryAttempt,
                    ServiceAddress = UriOrDefault(model.ServiceAddress),
                    InstanceAddress = UriOrDefault(model.InstanceAddress),
                };

                return instance;
            }
        }

        public class DbModel : ISaga
        {
            public Guid CorrelationId { get; set; }
            public int CurrentState { get; set; }

            public Guid JobId { get; set; }

            public DateTime? Started { get; set; }
            public DateTime? Faulted { get; set; }
            public Guid? StatusCheckTokenId { get; set; }

            public int RetryAttempt { get; set; }
            public string? ServiceAddress { get; set; }
            public string? InstanceAddress { get; set; }
        }
    }
}

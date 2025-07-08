namespace MassTransit.Dapper.Integration.JobSagas
{
    using Saga;


    public class JobTypeSagaDatabaseContext : JobSagaBaseContext<JobTypeSaga, JobTypeSagaDatabaseContext.DbModel>, DatabaseContext<JobTypeSaga>
    {
        public JobTypeSagaDatabaseContext(
            DatabaseContext<DbModel> databaseContext,
            SagaSerializer<JobTypeSaga, DbModel> serializer
        ) : base(databaseContext, serializer)
        { }

        public class Serializer : SystemTextJsonSagaSerializerBase<JobTypeSaga, DbModel>
        {
            public override DbModel FromSaga(JobTypeSaga instance)
            {
                return new DbModel
                {
                    CorrelationId = instance.CorrelationId,

                    Name = instance.Name,
                    CurrentState = instance.CurrentState,
                    ActiveJobCount = instance.ActiveJobCount,
                    ConcurrentJobLimit = instance.ConcurrentJobLimit,
                    OverrideJobLimit = instance.OverrideJobLimit,
                    OverrideLimitExpiration = instance.OverrideLimitExpiration,
                    GlobalConcurrentJobLimit = instance.GlobalConcurrentJobLimit,

                    ActiveJobs = Serialize(instance.ActiveJobs),
                    Instances = Serialize(instance.Instances),
                    Properties = Serialize(instance.Properties),
                };
            }

            public override JobTypeSaga? FromModel(DbModel? model)
            {
                if (model is null)
                    return null;

                return new JobTypeSaga
                {
                    CorrelationId = model.CorrelationId,

                    Name = model.Name!,
                    CurrentState = model.CurrentState,
                    ActiveJobCount = model.ActiveJobCount,
                    ConcurrentJobLimit = model.ConcurrentJobLimit,
                    OverrideJobLimit = model.OverrideJobLimit,
                    OverrideLimitExpiration = model.OverrideLimitExpiration,
                    GlobalConcurrentJobLimit = model.GlobalConcurrentJobLimit,

                    ActiveJobs = Deserialize<List<ActiveJob>>(model.ActiveJobs),
                    Instances = Deserialize<Dictionary<Uri, JobTypeInstance>>(model.Instances),
                    Properties = Deserialize<Dictionary<string, object>>(model.Properties),
                };
            }
        }

        public class DbModel : ISaga
        {
            public Guid CorrelationId { get; set; }
            public string? Name { get; set; }
            public int CurrentState { get; set; }

            public int ActiveJobCount { get; set; }
            public int ConcurrentJobLimit { get; set; }
            public int? OverrideJobLimit { get; set; }
            public DateTime? OverrideLimitExpiration { get; set; }
            public int? GlobalConcurrentJobLimit { get; set; }

            public string? ActiveJobs { get; set; }
            public string? Instances { get; set; }
            public string? Properties { get; set; }
        }
    }
}

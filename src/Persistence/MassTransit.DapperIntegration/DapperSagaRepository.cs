namespace MassTransit
{
    using System;
    using System.Data;
    using DapperIntegration.Saga;
    using Saga;


    public static class DapperSagaRepository<TSaga>
        where TSaga : class, ISaga
    {
        public static ISagaRepository<TSaga> Create(string connectionString, IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            var consumeContextFactory = new SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>();

            var options = new DapperOptions<TSaga>(connectionString, isolationLevel, null);
            var repositoryContextFactory = new DapperSagaRepositoryContextFactory<TSaga>(options, consumeContextFactory);

            return new SagaRepository<TSaga>(repositoryContextFactory, repositoryContextFactory, repositoryContextFactory);
        }

        public static ISagaRepository<TSaga> Create(string connectionString, Action<DapperOptions<TSaga>> configure = null)
        {
            var consumeContextFactory = new SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>();

            var options = new DapperOptions<TSaga>(connectionString, IsolationLevel.Serializable, null);
            configure?.Invoke(options);

            var repositoryContextFactory = new DapperSagaRepositoryContextFactory<TSaga>(options, consumeContextFactory);

            return new SagaRepository<TSaga>(repositoryContextFactory, repositoryContextFactory, repositoryContextFactory);
        }
    }
}

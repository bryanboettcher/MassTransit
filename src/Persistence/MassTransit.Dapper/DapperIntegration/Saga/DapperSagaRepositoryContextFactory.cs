namespace MassTransit.DapperIntegration.Saga
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper.Configuration;
    using Internals;
    using MassTransit.Saga;
    using Microsoft.Extensions.DependencyInjection;
    
    public class DapperSagaRepositoryContextFactory<TSaga> :
        ISagaRepositoryContextFactory<TSaga>,
        IQuerySagaRepositoryContextFactory<TSaga>,
        ILoadSagaRepositoryContextFactory<TSaga>
        where TSaga : class, ISaga
    {
        readonly ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> _factory;
        readonly IServiceProvider _serviceProvider;

        public DapperSagaRepositoryContextFactory(
            ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory,
            IServiceProvider serviceProvider)
        {
            _factory = factory;
            _serviceProvider = serviceProvider;
        }

        public Task<T> Execute<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken = default)
            where T : class
        {
            return ExecuteAsyncMethod(asyncMethod, cancellationToken);
        }

        public Task<T> Execute<T>(Func<QuerySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken = default)
            where T : class
        {
            return ExecuteAsyncMethod(asyncMethod, cancellationToken);
        }

        public void Probe(ProbeContext context)
        {
            context.Add("persistence", "dapper");
        }

        public async Task Send<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
            where T : class
        {
            await using var databaseContext = await CreateDatabaseContext(context.CancellationToken)
                .ConfigureAwait(false);

            var repositoryContext = new DapperSagaRepositoryContext<TSaga, T>(databaseContext, context, _factory);

            await next.Send(repositoryContext)
                .ConfigureAwait(false);

            // TODO: Handle transaction commit
        }

        public async Task SendQuery<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
            where T : class
        {
            await using var databaseContext = await CreateDatabaseContext(context.CancellationToken)
                .ConfigureAwait(false);

            var instances = await databaseContext.QueryAsync(query.FilterExpression, context.CancellationToken).ToListAsync()
                .ConfigureAwait(false);

            var repositoryContext = new DapperSagaRepositoryContext<TSaga, T>(databaseContext, context, _factory);
            var queryContext = new LoadedSagaRepositoryQueryContext<TSaga, T>(repositoryContext, instances);

            await next.Send(queryContext)
                .ConfigureAwait(false);

            // TODO: Handle transaction commit
        }

        async Task<T> ExecuteAsyncMethod<T>(Func<DapperSagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken)
            where T : class
        {
            await using var databaseContext = await CreateDatabaseContext(cancellationToken)
                .ConfigureAwait(false);

            var sagaRepositoryContext = new DapperSagaRepositoryContext<TSaga>(databaseContext, cancellationToken);

            var result = await asyncMethod(sagaRepositoryContext)
                .ConfigureAwait(false);

            // TODO: Handle transaction commit
            return result;
        }

        async Task<DatabaseContext<TSaga>> CreateDatabaseContext(CancellationToken cancellationToken)
        {
            var contextFactory = _serviceProvider.GetRequiredService<DatabaseContextFactory<TSaga>>();
            var context = contextFactory(_serviceProvider);
        }
    }
}

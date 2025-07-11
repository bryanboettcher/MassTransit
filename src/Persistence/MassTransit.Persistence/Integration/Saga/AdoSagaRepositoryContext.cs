namespace MassTransit.Persistence.Integration.Saga
{
    using MassTransit.Context;
    using MassTransit.Internals;
    using MassTransit.Middleware;
    using MassTransit.Saga;


    public class AdoSagaRepositoryContext<TSaga, TMessage> :
        ConsumeContextScope<TMessage>,
        SagaRepositoryContext<TSaga, TMessage>
        where TSaga : class, ISaga
        where TMessage : class
    {
        readonly ConsumeContext<TMessage> _consumeContext;
        readonly DatabaseContext<TSaga> _context;
        readonly ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> _factory;

        public AdoSagaRepositoryContext(DatabaseContext<TSaga> context, ConsumeContext<TMessage> consumeContext,
            ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory)
            : base(consumeContext, context)
        {
            _context = context;
            _consumeContext = consumeContext;
            _factory = factory;
        }

        public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContext<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
            where T : class
        {
            return _factory.CreateSagaConsumeContext(_context, consumeContext, instance, mode);
        }

        public Task<SagaConsumeContext<TSaga, TMessage>> Add(TSaga instance)
        {
            return _factory.CreateSagaConsumeContext(_context, _consumeContext, instance, SagaConsumeContextMode.Add);
        }

        public async Task<SagaConsumeContext<TSaga, TMessage>> Insert(TSaga instance)
        {
            await _context.InsertAsync(instance, CancellationToken)
                .ConfigureAwait(false);

            await _context.CommitAsync(CancellationToken)
                .ConfigureAwait(false);
            
            return await _factory.CreateSagaConsumeContext(_context, _consumeContext, instance, SagaConsumeContextMode.Insert).ConfigureAwait(false);
        }

        public async Task<SagaConsumeContext<TSaga, TMessage>> Load(Guid correlationId)
        {
            var instance = await _context.LoadAsync(correlationId, CancellationToken).ConfigureAwait(false);
            if (instance == null)
                return null;

            return await _factory.CreateSagaConsumeContext(_context, _consumeContext, instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
        }

        public Task Save(SagaConsumeContext<TSaga> context)
            => _context.InsertAsync(context.Saga, CancellationToken);

        public Task Update(SagaConsumeContext<TSaga> context)
            => _context.UpdateAsync(context.Saga, CancellationToken);

        public Task Delete(SagaConsumeContext<TSaga> context)
            => _context.DeleteAsync(context.Saga, CancellationToken);

        public Task Discard(SagaConsumeContext<TSaga> context)
            => Task.CompletedTask;

        public Task Undo(SagaConsumeContext<TSaga> context)
            => Task.CompletedTask;
    }

    public class AdoSagaRepositoryContext<TSaga> :
        BasePipeContext,
        QuerySagaRepositoryContext<TSaga>,
        LoadSagaRepositoryContext<TSaga>
        where TSaga : class, ISaga
    {
        readonly DatabaseContext<TSaga> _context;

        public AdoSagaRepositoryContext(DatabaseContext<TSaga> context, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _context = context;
        }

        public Task<TSaga> Load(Guid correlationId)
        {
            return _context.LoadAsync(correlationId, CancellationToken)!;
        }

        public async Task<SagaRepositoryQueryContext<TSaga>> Query(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
        {
            var instances = await (_context.QueryAsync(query.FilterExpression, cancellationToken).ToListAsync(cancellationToken))
                .ConfigureAwait(false);

            return new LoadedSagaRepositoryQueryContext<TSaga>(this, instances);
        }
    }
}

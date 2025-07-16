namespace MassTransit.Persistence.Tests.ComponentTests
{
    using System.Data;
    using System.Linq.Expressions;
    using Configuration;
    using Integration.Saga;
    using MassTransit.Tests.Pipeline;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Persistence.SqlServer.Configuration;
    using Persistence.SqlServer.Connections;
    using Persistence.SqlServer.Extensions;


    public class Configurator_Documentation_Tests
    {
        [Test]
        public void Registration_looks_correct()
        {
            var services = new ServiceCollection();
            services.AddMassTransit(bus =>
            {
                //bus.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
                //    .CustomRepository(conf => conf.UsingSqlServer(opt => opt
                //        .SetConnectionString("my connection string")
                //        .SetTableName("Orders")
                //        .SetIdentityColumnName("OrderId")
                //        .SetOptimisticConcurrency(m => m.RowVersion)
                //    ));

bus.AddScoped<DatabaseContext<OrderSaga>, OrderSagaRepository>();
bus.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
    .CustomRepository(conf => conf.SetContextFactory(
        async ctx => ctx.GetRequiredService<DatabaseContext<OrderSaga>>()
    ));

                bus.AddJobSagaStateMachines()
                    .CustomRepository(conf => conf.UsingSqlServer(
                        opt => opt.SetConnectionString("my connection string")
                    ));

                bus.UsingInMemory((ctx, cfg) =>
                {
                    cfg.UseMessageData(conf => conf.UsingSqlServer(
                        opt => opt.SetConnectionString("my connection string")
                    ));

                    cfg.ConfigureEndpoints(ctx);
                });
            });
        }
    }


    public class OrderSagaRepository : DatabaseContext<OrderSaga>
    {
        readonly IOrderService _service;
        public OrderSagaRepository(IOrderService service) => _service = service;

        public ValueTask DisposeAsync() => _service.DisposeAsync();

        public void Dispose() => _service.Dispose();

        public Task DeleteAsync(OrderSaga instance, CancellationToken cancellationToken = default)
            => _service.RemoveOrder(instance.CorrelationId, cancellationToken);

        public Task<OrderSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
            => _service.GetOrderById(correlationId, cancellationToken);

        public async Task InsertAsync(OrderSaga instance, CancellationToken cancellationToken = default)
            => _service.CreateOrder(instance, cancellationToken);

        public async Task UpdateAsync(OrderSaga instance, CancellationToken cancellationToken = default)
            => _service.UpdateOrder(instance, cancellationToken);

        public IAsyncEnumerable<OrderSaga> QueryAsync(Expression<Func<OrderSaga, bool>> filterExpression, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Orders do not need searches right now");

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }


    public class OrderSaga : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public byte[] RowVersion { get; set; }
        public DateTime CreatedOn { get; set; }
    }


    public class OrderStateMachine : MassTransitStateMachine<OrderSaga>
    {

    }
}

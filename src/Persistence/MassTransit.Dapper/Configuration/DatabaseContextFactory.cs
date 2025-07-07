namespace MassTransit.Dapper.Configuration;

using DapperIntegration.Saga;
using MassTransit.DapperIntegration.Saga;


public delegate Task<DatabaseContext<TSaga>> DatabaseContextFactory<TSaga>(IServiceProvider serviceProvider)
    where TSaga : class;

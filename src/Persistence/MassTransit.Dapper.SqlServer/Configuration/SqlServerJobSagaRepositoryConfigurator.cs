namespace MassTransit.Dapper.SqlServer.Configuration;

using System.Data;
using Dapper.Configuration;
using Formatting;
using MassTransit.Dapper.SqlServer.Connections;
using MassTransit.DapperIntegration.JobSagas;
using MassTransit.DapperIntegration.Saga;
using MassTransit.DapperIntegration.SqlBuilders;
using Microsoft.Extensions.DependencyInjection;


public class SqlServerJobSagaRepositoryConfigurator : ISqlServerJobSagaRepositoryConfigurator, ISpecification
{
    public string? ConnectionString { get; set; }
    
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure("ConnectionString must be specified");
    }

    public void Configure(IDapperJobSagaRepositoryConfigurator configurator)
    {
        (configurator as DapperJobSagaRepositoryConfigurator)?.AddCallback(RegisterDependencies);

        configurator.SetJobContextFactory(isp => Task.FromResult<DatabaseContext<JobSaga>>(isp.GetRequiredService<JobSagaDatabaseContext>()));
        configurator.SetJobTypeContextFactory(isp => Task.FromResult<DatabaseContext<JobTypeSaga>>(isp.GetRequiredService<JobTypeSagaDatabaseContext>()));
        configurator.SetJobAttemptContextFactory(isp => Task.FromResult<DatabaseContext<JobAttemptSaga>>(isp.GetRequiredService<JobAttemptSagaDatabaseContext>()));
    }
    
    public ISqlServerJobSagaRepositoryConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    void RegisterDependencies(IServiceCollection services)
    {
        Register<
            JobSagaDatabaseContext,
            JobSagaDatabaseContext.Serializer,
            JobSagaDatabaseContext.DbModel
        >();

        Register<
            JobAttemptSagaDatabaseContext,
            JobAttemptSagaDatabaseContext.Serializer,
            JobAttemptSagaDatabaseContext.DbModel
        >();

        Register<
            JobTypeSagaDatabaseContext,
            JobTypeSagaDatabaseContext.Serializer,
            JobTypeSagaDatabaseContext.DbModel
        >();

        return;

        void Register<TContext, TSerializer, TModel>()
            where TContext : class
            where TSerializer : class
            where TModel : class, ISaga
        {
            services.AddScoped<TContext>();
            services.AddScoped<TSerializer>();

            services.AddScoped<DatabaseContext<TModel>, SagaDatabaseContext<TModel>>();
            services.AddScoped<ISagaSqlFormatter<TModel>, PessimisticSqlServerSagaFormatter<TModel>>();

            services.AddScoped<ISagaConnectionProvider<TModel>>(
                _ => new SqlServerConnectionProvider<TModel>(ConnectionString!, IsolationLevel.ReadCommitted)
            );
        }
    }
}

namespace MassTransit.Dapper.PostgreSql.Configuration;

using System.Data;
using Connections;
using Formatting;
using MassTransit.Dapper.Configuration;
using MassTransit.DapperIntegration.JobSagas;
using MassTransit.DapperIntegration.Saga;
using MassTransit.DapperIntegration.SqlBuilders;
using Microsoft.Extensions.DependencyInjection;


public class PostgresJobSagaRepositoryConfigurator : IPostgresJobSagaRepositoryConfigurator, ISpecification
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
    
    public IPostgresJobSagaRepositoryConfigurator SetConnectionString(string connectionString)
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
            services.AddScoped<ISagaSqlFormatter<TModel>, PessimisticPostgresSagaFormatter<TModel>>();

            services.AddScoped<ISagaConnectionProvider<TModel>>(
                _ => new PostgresConnectionProvider<TModel>(ConnectionString!, IsolationLevel.ReadCommitted)
            );
        }
    }
}

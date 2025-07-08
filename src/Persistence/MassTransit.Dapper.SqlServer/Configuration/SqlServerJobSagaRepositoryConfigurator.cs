namespace MassTransit.Dapper.SqlServer.Configuration;

using System.Data;
using Dapper.Configuration;
using Formatting;
using Connections;
using Integration.JobSagas;
using Integration.Saga;
using Integration.SqlBuilders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


public class SqlServerJobSagaRepositoryConfigurator : ISqlServerJobSagaRepositoryConfigurator, ISpecification
{
    public string? ConnectionString { get; set; }
    
    public ISqlServerJobSagaRepositoryConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure("ConnectionString must be specified");
    }

    public void Configure(IAdoJobSagaRepositoryConfigurator configurator)
    {
        (configurator as AdoJobSagaRepositoryConfigurator)?.AddCallback(RegisterDependencies);

        configurator.SetJobContextFactory(Create<JobSaga, JobSagaDatabaseContext, JobSagaDatabaseContext.DbModel>());
        configurator.SetJobTypeContextFactory(Create<JobTypeSaga, JobTypeSagaDatabaseContext, JobTypeSagaDatabaseContext.DbModel>());
        configurator.SetJobAttemptContextFactory(Create<JobAttemptSaga, JobAttemptSagaDatabaseContext, JobAttemptSagaDatabaseContext.DbModel>());
    }

    void RegisterDependencies(IServiceCollection services)
    {
        services.TryAddScoped<JobSagaDatabaseContext>();
        services.TryAddScoped<SagaSerializer<JobSaga, JobSagaDatabaseContext.DbModel>, JobSagaDatabaseContext.Serializer>();
        services.TryAddScoped<ISagaSqlFormatter<JobSagaDatabaseContext.DbModel>>(
            _ => new PessimisticSqlServerSagaFormatter<JobSagaDatabaseContext.DbModel>("Jobs")
        );
        services.TryAddScoped<ISagaSqlConnectionProvider<JobSagaDatabaseContext.DbModel>>(
            _ => new SqlServerSqlConnectionProvider<JobSagaDatabaseContext.DbModel>(ConnectionString!, IsolationLevel.ReadCommitted)
        );

        services.TryAddScoped<JobAttemptSagaDatabaseContext>();
        services.TryAddScoped<SagaSerializer<JobAttemptSaga, JobAttemptSagaDatabaseContext.DbModel>, JobAttemptSagaDatabaseContext.Serializer>();
        services.TryAddScoped<ISagaSqlFormatter<JobAttemptSagaDatabaseContext.DbModel>>(
            _ => new PessimisticSqlServerSagaFormatter<JobAttemptSagaDatabaseContext.DbModel>("JobAttempts")
        );
        services.TryAddScoped<ISagaSqlConnectionProvider<JobAttemptSagaDatabaseContext.DbModel>>(
            _ => new SqlServerSqlConnectionProvider<JobAttemptSagaDatabaseContext.DbModel>(ConnectionString!, IsolationLevel.ReadCommitted)
        );

        services.TryAddScoped<JobTypeSagaDatabaseContext>();
        services.TryAddScoped<SagaSerializer<JobTypeSaga, JobTypeSagaDatabaseContext.DbModel>, JobTypeSagaDatabaseContext.Serializer>();
        services.TryAddScoped<ISagaSqlFormatter<JobTypeSagaDatabaseContext.DbModel>>(
            _ => new PessimisticSqlServerSagaFormatter<JobTypeSagaDatabaseContext.DbModel>("JobTypes")
        );
        services.TryAddScoped<ISagaSqlConnectionProvider<JobTypeSagaDatabaseContext.DbModel>>(
            _ => new SqlServerSqlConnectionProvider<JobTypeSagaDatabaseContext.DbModel>(ConnectionString!, IsolationLevel.ReadCommitted)
        );
    }

    static DatabaseContextFactory<TSaga> Create<TSaga, TContext, TModel>()
        where TSaga : class, ISaga
        where TContext : DatabaseContext<TSaga>
        where TModel : class, ISaga
    {
        return async serviceProvider =>
        {
            var formatter = serviceProvider.GetRequiredService<ISagaSqlFormatter<TModel>>();
            var serializer = serviceProvider.GetRequiredService<SagaSerializer<TSaga, TModel>>();
            var provider = serviceProvider.GetRequiredService<ISagaSqlConnectionProvider<TModel>>();

            var connection = await provider.CreateConnection();
            var context = new SagaDatabaseContext<TModel>(connection, formatter);
            return (TContext)Activator.CreateInstance(typeof(TContext), context, serializer)!;
        };
    }
}

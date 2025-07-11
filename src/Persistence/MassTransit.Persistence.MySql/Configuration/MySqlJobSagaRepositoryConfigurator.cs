namespace MassTransit.Persistence.MySql.Configuration;

using System.Data;
using Connections;
using Integration.JobSagas;
using Integration.Saga;
using Integration.SqlBuilders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Persistence.Configuration;


public class MySqlJobSagaRepositoryConfigurator : IMySqlJobSagaRepositoryConfigurator, ISpecification
{
    /// <inheritdoc />
    public string? ConnectionString { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure("ConnectionString must be specified");
    }

    /// <inheritdoc />
    public IMySqlJobSagaRepositoryConfigurator SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
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
        // TODO: Fix registrations

        //services.TryAddScoped<JobSagaDatabaseContext>();
        //services.TryAddScoped<SagaSerializer<JobSaga, JobSagaDatabaseContext.DbModel>, JobSagaDatabaseContext.Serializer>();
        //services.TryAddScoped<ISagaSqlFormatter<JobSagaDatabaseContext.DbModel>>(
        //    _ => new PessimisticMySqlSagaFormatter<JobSagaDatabaseContext.DbModel>("Jobs")
        //);
        //services.TryAddScoped<ISagaConnectionProvider<JobSagaDatabaseContext.DbModel>>(
        //    _ => new MySqlSagaConnectionProvider<JobSagaDatabaseContext.DbModel>(ConnectionString!, IsolationLevel.ReadCommitted)
        //);

        //services.TryAddScoped<JobAttemptSagaDatabaseContext>();
        //services.TryAddScoped<SagaSerializer<JobAttemptSaga, JobAttemptSagaDatabaseContext.DbModel>, JobAttemptSagaDatabaseContext.Serializer>();
        //services.TryAddScoped<ISagaSqlFormatter<JobAttemptSagaDatabaseContext.DbModel>>(
        //    _ => new PessimisticMySqlSagaFormatter<JobAttemptSagaDatabaseContext.DbModel>("JobAttempts")
        //);
        //services.TryAddScoped<ISagaConnectionProvider<JobAttemptSagaDatabaseContext.DbModel>>(
        //    _ => new MySqlSagaConnectionProvider<JobAttemptSagaDatabaseContext.DbModel>(ConnectionString!, IsolationLevel.ReadCommitted)
        //);

        //services.TryAddScoped<JobTypeSagaDatabaseContext>();
        //services.TryAddScoped<SagaSerializer<JobTypeSaga, JobTypeSagaDatabaseContext.DbModel>, JobTypeSagaDatabaseContext.Serializer>();
        //services.TryAddScoped<ISagaSqlFormatter<JobTypeSagaDatabaseContext.DbModel>>(
        //    _ => new PessimisticMySqlSagaFormatter<JobTypeSagaDatabaseContext.DbModel>("JobTypes")
        //);
        //services.TryAddScoped<ISagaConnectionProvider<JobTypeSagaDatabaseContext.DbModel>>(
        //    _ => new MySqlSagaConnectionProvider<JobTypeSagaDatabaseContext.DbModel>(ConnectionString!, IsolationLevel.ReadCommitted)
        //);
    }

    static DatabaseContextFactory<TSaga> Create<TSaga, TContext, TModel>()
        where TSaga : class, ISaga
        where TContext : DatabaseContext<TSaga>
        where TModel : class, ISaga
    {
        // TODO: Fix registrations
        return async serviceProvider => null;
        //{
        //    var formatter = serviceProvider.GetRequiredService<ISagaSqlFormatter<TModel>>();
        //    var serializer = serviceProvider.GetRequiredService<SagaSerializer<TSaga, TModel>>();
        //    var provider = serviceProvider.GetRequiredService<ISagaConnectionProvider<TModel>>();

        //    var connection = await provider.CreateConnection();
        //    var context = new SagaDatabaseContext<TModel>(connection, formatter);
        //    return (TContext)Activator.CreateInstance(typeof(TContext), context, serializer)!;
        //};
    }
}

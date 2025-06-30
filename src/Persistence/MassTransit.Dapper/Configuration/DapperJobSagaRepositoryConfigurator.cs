namespace MassTransit.Configuration;

using System.Data;
using System.Data.Common;
using DapperIntegration.JobSagas;
using DapperIntegration.Saga;
using DapperIntegration.SqlBuilders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Saga;


public class DapperJobSagaRepositoryConfigurator : IDapperJobSagaRepositoryConfigurator
{
    Func<IServiceProvider, DatabaseContextFactory<JobSaga>>? _jobContextFactory;
    Func<IServiceProvider, DatabaseContextFactory<JobTypeSaga>>? _jobTypeContextFactory;
    Func<IServiceProvider, DatabaseContextFactory<JobAttemptSaga>>? _jobAttemptContextFactory;

    protected DatabaseProviders Provider;
    protected string? ConnectionString { get; set; }
    protected string? TableName { get; set; }
    protected IsolationLevel? IsolationLevel { get; set; }

    public void UseJobContextFactory(Func<IServiceProvider, DatabaseContextFactory<JobSaga>> factoryFunc)
        => _jobContextFactory = factoryFunc;

    public void UseJobTypeContextFactory(Func<IServiceProvider, DatabaseContextFactory<JobTypeSaga>> factoryFunc)
        => _jobTypeContextFactory = factoryFunc;

    public void UseJobAttemptContextFactory(Func<IServiceProvider, DatabaseContextFactory<JobAttemptSaga>> factoryFunc)
        => _jobAttemptContextFactory = factoryFunc;

    public void UseSqlServer(string connectionString)
    {
        ConnectionString = connectionString;
        Provider = DatabaseProviders.SqlServer;
    }

    public void UsePostgres(string connectionString)
    {
        ConnectionString = connectionString;
        Provider = DatabaseProviders.Postgres;
    }

    public void UseIsolationLevel(IsolationLevel isolationLevel)
        => IsolationLevel = isolationLevel;


    internal void RegisterJob<TSaga>(ISagaRepositoryRegistrationConfigurator<TSaga> configurator) where TSaga : JobSaga
    {
        if (configurator is not ISagaRepositoryRegistrationConfigurator<JobSaga> cfg)
            throw new InvalidOperationException("RegisterJob should only be called with JobSaga");

        configurator.TryAddScoped<DatabaseContext<DbJobModel>, SagaDatabaseContext<DbJobModel>>();
        configurator.TryAddScoped<DapperSagaSerializer<JobSaga, DbJobModel>, JobSerializer>();

        RegisterRepositories(cfg, conf => conf.ContextFactoryProvider ??= _jobContextFactory ?? BuildContext());
        return;

        Func<IServiceProvider, DatabaseContextFactory<JobSaga>> BuildContext()
        {
            return sp => (c, t) =>
            {
                var options = sp.GetRequiredService<IOptions<DapperOptions<JobSaga>>>().Value;
                ISagaSqlFormatter<DbJobModel> formatter = options.Provider switch
                {
                    DatabaseProviders.Postgres => new PostgresSagaFormatter<DbJobModel>(options.TableName ?? "Jobs", options.IdColumnName),
                    DatabaseProviders.SqlServer => new SqlServerSagaFormatter<DbJobModel>(options.TableName ?? "Jobs", options.IdColumnName),
                    _ => throw new InvalidOperationException("JobSagas only support SqlServer or Postgres without a custom ContextFactoryProvider")
                };
                var context = new SagaDatabaseContext<DbJobModel>(c, t, formatter);
                var serializer = sp.GetRequiredService<DapperSagaSerializer<JobSaga, DbJobModel>>();

                return new JobSagaDatabaseContext(context, serializer);
            };
        }
    }

    internal void RegisterJobType<TSaga>(ISagaRepositoryRegistrationConfigurator<TSaga> configurator) where TSaga : JobTypeSaga
    {
        if (configurator is not ISagaRepositoryRegistrationConfigurator<JobTypeSaga> cfg)
            throw new InvalidOperationException("RegisterJobType should only be called with JobTypeSaga");

        configurator.TryAddScoped<DatabaseContext<DbJobTypeModel>, SagaDatabaseContext<DbJobTypeModel>>();
        configurator.TryAddScoped<DapperSagaSerializer<JobTypeSaga, DbJobTypeModel>, JobTypeSerializer>();

        RegisterRepositories(cfg, conf => conf.ContextFactoryProvider ??= _jobTypeContextFactory ?? BuildContext());
        return;

        Func<IServiceProvider, DatabaseContextFactory<JobTypeSaga>> BuildContext()
        {
            return sp => (c, t) =>
            {
                var options = sp.GetRequiredService<IOptions<DapperOptions<JobTypeSaga>>>().Value;
                ISagaSqlFormatter<DbJobTypeModel> formatter = options.Provider switch
                {
                    DatabaseProviders.Postgres => new PostgresSagaFormatter<DbJobTypeModel>(options.TableName ?? "JobTypes", options.IdColumnName),
                    DatabaseProviders.SqlServer => new SqlServerSagaFormatter<DbJobTypeModel>(options.TableName ?? "JobTypes", options.IdColumnName),
                    _ => throw new InvalidOperationException("JobSagas only support SqlServer or Postgres without a custom ContextFactoryProvider")
                };
                var context = new SagaDatabaseContext<DbJobTypeModel>(c, t, formatter);
                var serializer = sp.GetRequiredService<DapperSagaSerializer<JobTypeSaga, DbJobTypeModel>>();

                return new JobTypeSagaDatabaseContext(context, serializer);
            };
        }
    }

    internal void RegisterJobAttempt<TSaga>(ISagaRepositoryRegistrationConfigurator<TSaga> configurator) where TSaga : JobAttemptSaga
    {
        if (configurator is not ISagaRepositoryRegistrationConfigurator<JobAttemptSaga> cfg)
            throw new InvalidOperationException("RegisterJobAttempt should only be called with JobAttemptSaga");

        configurator.TryAddScoped<DatabaseContext<DbJobAttemptModel>, SagaDatabaseContext<DbJobAttemptModel>>();
        configurator.TryAddScoped<DapperSagaSerializer<JobAttemptSaga, DbJobAttemptModel>, JobAttemptSerializer>();
        configurator.TryAddScoped<ISagaSqlFormatter<DbJobAttemptModel>, SqlServerSagaFormatter<DbJobAttemptModel>>();

        RegisterRepositories(cfg, conf => conf.ContextFactoryProvider ??= _jobAttemptContextFactory ?? BuildContext());
        return;

        Func<IServiceProvider, DatabaseContextFactory<JobAttemptSaga>> BuildContext()
        {
            return sp => (c, t) =>
            {
                var options = sp.GetRequiredService<IOptions<DapperOptions<JobAttemptSaga>>>().Value;
                ISagaSqlFormatter<DbJobAttemptModel> formatter = options.Provider switch
                {
                    DatabaseProviders.Postgres => new PostgresSagaFormatter<DbJobAttemptModel>(options.TableName ?? "JobAttempts", options.IdColumnName),
                    DatabaseProviders.SqlServer => new SqlServerSagaFormatter<DbJobAttemptModel>(options.TableName ?? "JobAttempts", options.IdColumnName),
                    _ => throw new InvalidOperationException("JobSagas only support SqlServer or Postgres without a custom ContextFactoryProvider")
                };
                var context = new SagaDatabaseContext<DbJobAttemptModel>(c, t, formatter);
                var serializer = sp.GetRequiredService<DapperSagaSerializer<JobAttemptSaga, DbJobAttemptModel>>();

                return new JobAttemptSagaDatabaseContext(context, serializer);
            };
        }
    }

    void RegisterRepositories<TSaga>(
        ISagaRepositoryRegistrationConfigurator<TSaga> configurator,
        Action<DapperOptions<TSaga>> configure
    ) where TSaga : class, ISaga
    {
        // TODO: Consolidate with DapperSagaRepositoryConfigurator!
        configurator.AddOptions<DapperOptions<TSaga>>().Configure(opt =>
        {
            if (Provider != DatabaseProviders.Unspecified && opt.Provider != Provider)
                opt.Provider = Provider;

            opt.ConnectionString ??= ConnectionString;
            opt.IsolationLevel ??= IsolationLevel;
            opt.TableName ??= TableName;
            opt.DbConnectionProvider ??= BuildConnectionFactory<TSaga>();
        }).Configure(configure);

        configurator.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
        configurator.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
        configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>,
            DapperSagaRepositoryContextFactory<TSaga>>();
    }

    Func<IServiceProvider, DbConnection> BuildConnectionFactory<TSaga>()
        where TSaga : class, ISaga
    {
        return Provider == DatabaseProviders.Postgres
            ? Postgres()
            : SqlServer();

        static Func<IServiceProvider, DbConnection> SqlServer() =>
            sp =>
            {
                var options = sp.GetRequiredService<IOptions<DapperOptions<TSaga>>>().Value;
                return new SqlConnection(options.ConnectionString);
            };

        static Func<IServiceProvider, DbConnection> Postgres() =>
            sp =>
            {
                var options = sp.GetRequiredService<IOptions<DapperOptions<TSaga>>>().Value;
                return new NpgsqlConnection(options.ConnectionString);
            };
    }
}
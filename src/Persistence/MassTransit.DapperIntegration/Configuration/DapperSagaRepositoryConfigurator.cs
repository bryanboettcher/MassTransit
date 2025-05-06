#nullable enable

namespace MassTransit.Configuration
{
    using System.Collections.Generic;
    using System.Data;
    using DapperIntegration.Saga;
    using DapperIntegration.SqlBuilders;
    using Saga;
    using System;
    using DapperIntegration.JobSagas;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;

    public class DapperSagaRepositoryConfigurator : IDapperSagaRepositoryConfigurator, ISpecification
    {
        protected DatabaseProviders Provider = DatabaseProviders.Unspecified;

        protected string? ConnectionString { get; set; }
        protected string? TableName { get; set; }
        protected string? IdColumnName { get; set; }
        protected IsolationLevel? IsolationLevel { get; set; }
        
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
        {
            IsolationLevel = isolationLevel;
        }

        public void UseTableName(string tableName)
        {
            TableName = tableName;
        }

        public void UseIdColumnName(string idColumnName)
        {
            IdColumnName = idColumnName;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                yield return this.Failure("ConnectionString", "must be specified");
        }
    }
    
    public enum DatabaseProviders
    {
        Unspecified,
        SqlServer,
        Postgres
    }
    
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
        
        public void RegisterJob<TSaga>(ISagaRepositoryRegistrationConfigurator<TSaga> configurator) where TSaga : JobSaga
        {
            if (configurator is not ISagaRepositoryRegistrationConfigurator<JobSaga> cfg)
                throw new InvalidOperationException("RegisterJob should only be called with JobSaga");

            configurator.TryAddScoped<DatabaseContext<DbJobModel>, SagaDatabaseContext<DbJobModel>>();
            configurator.TryAddScoped<DapperSagaSerializer<JobSaga, DbJobModel>, JobSerializer>();

            RegisterRepositories(cfg, conf => conf.ContextFactoryProvider ??= BuildContext());
            return;

            Func<IServiceProvider, DatabaseContextFactory<JobSaga>> BuildContext()
            {
                return sp => (c, t) =>
                {
                    var options = sp.GetRequiredService<IOptions<DapperOptions<JobSaga>>>().Value;
                    SqlBuilder<DbJobModel> builder = options.Provider switch
                    {
                        DatabaseProviders.Postgres => new PostgresBuilder<DbJobModel>(options.TableName, options.IdColumnName),
                        DatabaseProviders.SqlServer => new SqlServerBuilder<DbJobModel>(options.TableName, options.IdColumnName),
                        _ => throw new InvalidOperationException("JobSagas only support SqlServer or Postgres without a custom ContextFactoryProvider")
                    };
                    var context = new SagaDatabaseContext<DbJobModel>(c, t, builder);
                    var serializer = sp.GetRequiredService<DapperSagaSerializer<JobSaga, DbJobModel>>();

                    return new JobSagaDatabaseContext(context, serializer);
                };
            }
        }

        public void RegisterJobType<TSaga>(ISagaRepositoryRegistrationConfigurator<TSaga> configurator) where TSaga : JobTypeSaga
        {
            if (configurator is not ISagaRepositoryRegistrationConfigurator<JobTypeSaga> cfg)
                throw new InvalidOperationException("RegisterJobType should only be called with JobTypeSaga");

            configurator.TryAddScoped<DatabaseContext<DbJobTypeModel>, SagaDatabaseContext<DbJobTypeModel>>();
            configurator.TryAddScoped<DapperSagaSerializer<JobTypeSaga, DbJobTypeModel>, JobTypeSerializer>();
            
            RegisterRepositories(cfg, conf => conf.ContextFactoryProvider ??= BuildContext());
            return;

            Func<IServiceProvider, DatabaseContextFactory<JobTypeSaga>> BuildContext()
            {
                return sp => (c, t) =>
                {
                    var options = sp.GetRequiredService<IOptions<DapperOptions<JobTypeSaga>>>().Value;
                    SqlBuilder<DbJobTypeModel> builder = options.Provider switch
                    {
                        DatabaseProviders.Postgres => new PostgresBuilder<DbJobTypeModel>(options.TableName, options.IdColumnName),
                        DatabaseProviders.SqlServer => new SqlServerBuilder<DbJobTypeModel>(options.TableName, options.IdColumnName),
                        _ => throw new InvalidOperationException("JobSagas only support SqlServer or Postgres without a custom ContextFactoryProvider")
                    };
                    var context = new SagaDatabaseContext<DbJobTypeModel>(c, t, builder);
                    var serializer = sp.GetRequiredService<DapperSagaSerializer<JobTypeSaga, DbJobTypeModel>>();

                    return new JobTypeSagaDatabaseContext(context, serializer);
                };
            }
        }

        public void RegisterJobAttempt<TSaga>(ISagaRepositoryRegistrationConfigurator<TSaga> configurator) where TSaga : JobAttemptSaga
        {
            if (configurator is not ISagaRepositoryRegistrationConfigurator<JobAttemptSaga> cfg)
                throw new InvalidOperationException("RegisterJobAttempt should only be called with JobAttemptSaga");

            configurator.TryAddScoped<DatabaseContext<DbJobAttemptModel>, SagaDatabaseContext<DbJobAttemptModel>>();
            configurator.TryAddScoped<DapperSagaSerializer<JobAttemptSaga, DbJobAttemptModel>, JobAttemptSerializer>();
            configurator.TryAddScoped<SqlBuilder<DbJobAttemptModel>, SqlServerBuilder<DbJobAttemptModel>>();

            RegisterRepositories(cfg, conf => conf.ContextFactoryProvider ??= BuildContext());
            return;

            Func<IServiceProvider, DatabaseContextFactory<JobAttemptSaga>> BuildContext()
            {
                return sp => (c, t) =>
                {
                    var options = sp.GetRequiredService<IOptions<DapperOptions<JobAttemptSaga>>>().Value;
                    SqlBuilder<DbJobAttemptModel> builder = options.Provider switch
                    {
                        DatabaseProviders.Postgres => new PostgresBuilder<DbJobAttemptModel>(options.TableName, options.IdColumnName),
                        DatabaseProviders.SqlServer => new SqlServerBuilder<DbJobAttemptModel>(options.TableName, options.IdColumnName),
                        _ => throw new InvalidOperationException("JobSagas only support SqlServer or Postgres without a custom ContextFactoryProvider")
                    };
                    var context = new SagaDatabaseContext<DbJobAttemptModel>(c, t, builder);
                    var serializer = sp.GetRequiredService<DapperSagaSerializer<JobAttemptSaga, DbJobAttemptModel>>();

                    return new JobAttemptSagaDatabaseContext(context, serializer);
                };
            }
        }

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
        {
            IsolationLevel = isolationLevel;
        }

        private void RegisterRepositories<TSaga>(
            ISagaRepositoryRegistrationConfigurator<TSaga> configurator,
            Action<DapperOptions<TSaga>> configure
        ) where TSaga : class, ISaga
        {
            configurator.AddOptions<DapperOptions<TSaga>>().Configure(opt =>
            {
                opt.ConnectionString ??= ConnectionString;
                opt.IsolationLevel ??= IsolationLevel;
                opt.TableName ??= TableName;
                opt.Provider = Provider;
            }).Configure(configure);

            configurator.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>,
                DapperSagaRepositoryContextFactory<TSaga>>();
        }
    }

    public class DapperSagaRepositoryConfigurator<TSaga> : DapperSagaRepositoryConfigurator,
        IDapperSagaRepositoryConfigurator<TSaga>
        where TSaga : class, ISaga
    {
        // only used to construct inline ContextProvider funcs
        DatabaseContextFactory<TSaga>? _contextFactory;

        public DapperSagaRepositoryConfigurator()
        {
            ContextFactoryProvider = null;
            SqlBuilderProvider = null;
        }

        public DapperSagaRepositoryConfigurator(string? connectionString = null, IsolationLevel? isolationLevel = null) : this()
        {
            if (connectionString is not null)
                ConnectionString = connectionString;

            if (isolationLevel is not null)
                IsolationLevel = isolationLevel;
        }

        protected Func<IServiceProvider, SqlBuilder<TSaga>>? SqlBuilderProvider { get; set; }
        protected Func<IServiceProvider, DatabaseContextFactory<TSaga>>? ContextFactoryProvider { get; set; }

        public DatabaseContextFactory<TSaga>? ContextFactory
        {
            get => _contextFactory!;
            set
            {
                _contextFactory = value;

                if (value is not null)
                    ContextFactoryProvider = _ => value;
            }
        }

        public void UseSqlBuilder(Func<IServiceProvider, SqlBuilder<TSaga>> factory)
            => SqlBuilderProvider = factory;

        public void UseContextFactory(Func<IServiceProvider, DatabaseContextFactory<TSaga>> factory)
            => ContextFactoryProvider = factory;

        public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
        {
            // Because there are existing implementations in the wild, the context
            // and sql builders have to be very carefully chosen to avoid breaking
            // anything that might be relying on unexpected behavior of the legacy
            // context.  If any of the "new" properties are set, this is new code
            // and would not need to rely on behavior of the legacy context.
            // 
            // This is accounted for in BuildContextFactory below, and
            // DapperSagaRepositoryContextFactory:CreateDatabaseContext.
            var contextFactory = BuildContextFactory();
            
            configurator.AddOptions<DapperOptions<TSaga>>().Configure(opt =>
            {
                opt.ConnectionString ??= ConnectionString;
                opt.IsolationLevel ??= IsolationLevel;
                opt.IdColumnName ??= IdColumnName;
                opt.TableName ??= TableName;
                opt.Provider = Provider;
                opt.SqlBuilderProvider ??= SqlBuilderProvider;
                opt.ContextFactoryProvider ??= contextFactory;
            });

            configurator.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>, 
                DapperSagaRepositoryContextFactory<TSaga>>();
        }
        
        private Func<IServiceProvider, DatabaseContextFactory<TSaga>>? BuildContextFactory()
        {
            // someone made it easy for us again
            if (ContextFactoryProvider is not null)
                return ContextFactoryProvider;

            // legacy setting override
            if (ContextFactory is not null)
                return _ => ContextFactory;

            // the Provider is set when calling UseSqlServer or UsePostgres, so if
            // it's not set here, then we'll have to resort to the legacy context.
            if (Provider == DatabaseProviders.Unspecified)
                return _ => (c, t) => new DapperDatabaseContext<TSaga>(c, t);

            // null is a special case here, causing resolution with the new
            // SagaDatabaseContext<T> in the RepositoryContextFactory
            return null;
        }
    }
}

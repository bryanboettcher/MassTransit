namespace MassTransit;

using System.Data;
using System.Data.Common;
using Configuration;
using DapperIntegration.Saga;
using DapperIntegration.SqlBuilders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saga;


public class DapperSagaRepositoryConfigurator<TSaga> : DapperSagaRepositoryConfigurator,
    IDapperRepositoryConfigurator<TSaga>
    where TSaga : class, ISaga
{
    public DapperSagaRepositoryConfigurator()
    {
        ContextFactoryProvider = null;
        SqlBuilderProvider = null;
    }

    [Obsolete("Configure the repository via UseXXX() methods", false)]
    public DapperSagaRepositoryConfigurator(string? connectionString = null, IsolationLevel? isolationLevel = null)
        : this()
    {
        if (connectionString is not null)
            ConnectionString = connectionString;

        if (isolationLevel is not null)
            IsolationLevel = isolationLevel;
    }

    protected Func<IServiceProvider, ISagaSqlFormatter<TSaga>>? SqlBuilderProvider { get; set; }
    protected Func<IServiceProvider, DatabaseContextFactory<TSaga>>? ContextFactoryProvider { get; set; }

    [Obsolete("Configure the ContextFactory via UseContextFactory()", false)]
    public DatabaseContextFactory<TSaga>? ContextFactory { get; set; }

    public void SetSqlFormatter(Func<IServiceProvider, ISagaSqlFormatter<TSaga>> factory) => SqlBuilderProvider = factory;

    public void SetContextFactory(Func<IServiceProvider, DatabaseContextFactory<TSaga>> factory) => ContextFactoryProvider = factory;

    internal void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
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
        var connectionFactory = BuildConnectionFactory();

        // TODO: Consolidate with DapperJobSagaRepositoryConfigurator!
        configurator.AddOptions<DapperOptions<TSaga>>().Configure(opt =>
        {
            if (Provider != DatabaseProviders.Unspecified && opt.Provider != Provider)
                opt.Provider = Provider;

            opt.ConnectionString ??= ConnectionString;
            opt.IsolationLevel ??= IsolationLevel;
            opt.IdColumnName ??= IdColumnName;
            opt.TableName ??= TableName;

            opt.SqlBuilderProvider ??= SqlBuilderProvider;
            opt.ContextFactoryProvider ??= contextFactory;
            opt.DbConnectionProvider ??= connectionFactory;
        });

        configurator.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
        configurator.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
        configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>,
            DapperSagaRepositoryContextFactory<TSaga>>();
    }

    Func<IServiceProvider, DatabaseContextFactory<TSaga>>? BuildContextFactory()
    {
        // someone made it easy for us again
        if (ContextFactoryProvider is not null)
            return ContextFactoryProvider;

        // legacy setting override
        #pragma warning disable CS0618 // Type or member is obsolete
        if (ContextFactory is not null)
            return _ => ContextFactory;
        #pragma warning restore CS0618 // Type or member is obsolete

        // null is a special case here, causing resolution with the new
        // SagaDatabaseContext<T> in the RepositoryContextFactory
        return null;
    }

    Func<IServiceProvider, DbConnection> BuildConnectionFactory()
    {
        if (DbConnectionProvider is not null)
            return DbConnectionProvider;

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
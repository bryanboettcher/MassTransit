namespace MassTransit.Dapper.Configuration;

using DapperIntegration.Saga;
using DapperIntegration.SqlBuilders;
using MassTransit.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Saga;

public class DapperRepositoryConfigurator<TSaga> : IDapperRepositoryConfigurator<TSaga>, ISpecification
    where TSaga : class, ISaga
{
    DatabaseContextFactory<TSaga>? _contextFactory;
    ISagaSqlFormatter<TSaga>? _formatter;
    ISagaConnectionProvider<TSaga>? _connectionProvider;

    /// <inheritdoc />
    public IDapperRepositoryConfigurator<TSaga> SetContextFactory(DatabaseContextFactory<TSaga> contextFactory)
    {
        _contextFactory = contextFactory;
        return this;
    }

    /// <inheritdoc />
    public IDapperRepositoryConfigurator<TSaga> SetSqlFormatter(ISagaSqlFormatter<TSaga> formatter)
    {
        _formatter = formatter;
        return this;
    }

    /// <inheritdoc />
    public IDapperRepositoryConfigurator<TSaga> SetConnectionProvider(ISagaConnectionProvider<TSaga> connectionProvider)
    {
        _connectionProvider = connectionProvider;
        return this;
    }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate()
    {
        if (_contextFactory is null)
            yield return this.Failure("ContextFactory must be set");
    }

    public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> services)
    {
        services.TryAddScoped(_ => _formatter!);
        services.TryAddScoped(_ => _connectionProvider!);

        services.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
        services.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
        services.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>, DapperSagaRepositoryContextFactory<TSaga>>();
    }
}

namespace MassTransit.Dapper.Configuration;

using DapperIntegration.Saga;
using MassTransit.Configuration;
using MassTransit.DapperIntegration.Saga;
using Microsoft.Extensions.DependencyInjection;
using Saga;

public class DapperRepositoryConfigurator<TSaga> : IDapperRepositoryConfigurator<TSaga>, ISpecification
    where TSaga : class, ISaga
{
    readonly List<Action<IServiceCollection>> _callbacks = new();

    DatabaseContextFactory<TSaga>? _contextFactory;
    
    /// <inheritdoc />
    public IDapperRepositoryConfigurator<TSaga> SetContextFactory(DatabaseContextFactory<TSaga> contextFactory)
    {
        _contextFactory = contextFactory;
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
        _callbacks.ForEach(c => c.Invoke(services));
        _callbacks.Clear();

        services.AddScoped(_ => _contextFactory!);

        services.RegisterLoadSagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
        services.RegisterQuerySagaRepository<TSaga, DapperSagaRepositoryContextFactory<TSaga>>();
        services.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>, DapperSagaRepositoryContextFactory<TSaga>>();
    }

    public void AddCallback(Action<IServiceCollection> callback)
        => _callbacks.Add(callback);
}

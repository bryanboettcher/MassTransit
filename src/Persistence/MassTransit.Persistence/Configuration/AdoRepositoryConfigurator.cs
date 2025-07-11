namespace MassTransit.Persistence.Configuration;

using Integration.Saga;
using MassTransit.Configuration;
using MassTransit.Saga;
using Microsoft.Extensions.DependencyInjection;


public class AdoRepositoryConfigurator<TSaga> : IAdoRepositoryConfigurator<TSaga>, ISpecification
    where TSaga : class, ISaga
{
    readonly List<Action<IServiceCollection>> _callbacks = new();

    DatabaseContextFactory<TSaga>? _contextFactory;
    
    /// <inheritdoc />
    public IAdoRepositoryConfigurator<TSaga> SetContextFactory(DatabaseContextFactory<TSaga> contextFactory)
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

        services.RegisterLoadSagaRepository<TSaga, AdoSagaRepositoryContextFactory<TSaga>>();
        services.RegisterQuerySagaRepository<TSaga, AdoSagaRepositoryContextFactory<TSaga>>();
        services.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>, AdoSagaRepositoryContextFactory<TSaga>>();
    }

    public void AddCallback(Action<IServiceCollection> callback)
        => _callbacks.Add(callback);
}

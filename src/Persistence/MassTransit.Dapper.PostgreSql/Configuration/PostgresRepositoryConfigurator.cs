namespace MassTransit.Dapper.PostgreSql.Configuration;

using System.Data;
using Connections;
using Formatting;
using Integration.Saga;
using Integration.SqlBuilders;
using MassTransit.Dapper.Configuration;
using Microsoft.Extensions.DependencyInjection;


public class PostgresRepositoryConfigurator<TSaga> : IPostgresRepositoryConfigurator<TSaga>, ISpecification
    where TSaga : class, ISaga
{
    /// <inheritdoc />
    public string? ConnectionString { get; set; }

    /// <inheritdoc />
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

    /// <inheritdoc />
    public ConcurrencyMode ConcurrencyMode { get; set; } = ConcurrencyMode.Pessimistic;

    /// <inheritdoc />
    public string VersionColumnName { get; set; }

    /// <inheritdoc />
    public string? TableName { get; set; }

    /// <inheritdoc />
    public string IdentityColumnName { get; set; } = "CorrelationId";

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure($"{nameof(ConnectionString)} must be set");

        if (ConcurrencyMode == ConcurrencyMode.Optimistic && string.IsNullOrWhiteSpace(VersionColumnName))
            yield return this.Failure($"{nameof(VersionColumnName)} must be set when using Optimistic concurrency");
    }

    /// <inheritdoc />
    public IPostgresRepositoryConfigurator<TSaga> SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    /// <inheritdoc />
    public IPostgresRepositoryConfigurator<TSaga> SetTableName(string tableName)
    {
        TableName = tableName;
        return this;
    }

    /// <inheritdoc />
    public IPostgresRepositoryConfigurator<TSaga> SetIdentityColumnName(string identityColumnName)
    {
        IdentityColumnName = identityColumnName;
        return this;
    }

    /// <inheritdoc />
    public IPostgresRepositoryConfigurator<TSaga> SetOptimisticConcurrency(string versionColumnName = "xmin")
    {
        ConcurrencyMode = ConcurrencyMode.Optimistic;
        VersionColumnName = versionColumnName;
        return this;
    }

    /// <inheritdoc />
    public IPostgresRepositoryConfigurator<TSaga> SetPessimisticConcurrency(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        ConcurrencyMode = ConcurrencyMode.Pessimistic;
        IsolationLevel = isolationLevel;
        return this;
    }

    public void Configure(IAdoRepositoryConfigurator<TSaga> sagaConfigurator)
    {
        (sagaConfigurator as AdoRepositoryConfigurator<TSaga>)?
            .AddCallback(RegisterServices);

        sagaConfigurator.SetContextFactory(ConfiguredContextFactory);
    }
    
    ISagaSqlFormatter<TSaga> ConfiguredFormatter() => ConcurrencyMode == ConcurrencyMode.Optimistic
        ? new OptimisticPostgresSagaFormatter<TSaga>(TableName, IdentityColumnName, VersionColumnName)
        : new PessimisticPostgresSagaFormatter<TSaga>(TableName, IdentityColumnName);

    ISagaConnectionProvider<TSaga> ConfiguredConnectionProvider() =>
        new PostgresSagaConnectionProvider<TSaga>(ConnectionString!, ConcurrencyMode == ConcurrencyMode.Optimistic ? null : IsolationLevel);

    static async Task<DatabaseContext<TSaga>> ConfiguredContextFactory(IServiceProvider serviceProvider)
    {
        var formatter = serviceProvider.GetRequiredService<ISagaSqlFormatter<TSaga>>();
        var connectionProvider = serviceProvider.GetRequiredService<ISagaConnectionProvider<TSaga>>();

        var connection = await connectionProvider.CreateConnection();
        return new SagaDatabaseContext<TSaga>(connection, formatter);
    }

    void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<DatabaseContext<TSaga>, SagaDatabaseContext<TSaga>>();

        services.AddSingleton(ConfiguredFormatter());
        services.AddSingleton(ConfiguredConnectionProvider());
    }
}

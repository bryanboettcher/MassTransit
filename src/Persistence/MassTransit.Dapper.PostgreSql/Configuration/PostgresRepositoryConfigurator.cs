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
    public string? ConnectionString { get; set; }
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
    public ConcurrencyMode ConcurrencyMode { get; set; } = ConcurrencyMode.Pessimistic;
    public string VersionColumnName { get; set; }
    public string? TableName { get; set; }
    public string IdentityColumnName { get; set; }

    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure($"{nameof(ConnectionString)} must be set");

        if (ConcurrencyMode == ConcurrencyMode.Optimistic && string.IsNullOrWhiteSpace(VersionColumnName))
            yield return this.Failure($"{nameof(VersionColumnName)} must be set when using Optimistic concurrency");
    }

    public IPostgresRepositoryConfigurator<TSaga> SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    public IPostgresRepositoryConfigurator<TSaga> SetTableName(string tableName)
    {
        TableName = tableName;
        return this;
    }

    public IPostgresRepositoryConfigurator<TSaga> SetIdentityColumnName(string identityColumnName)
    {
        IdentityColumnName = identityColumnName;
        return this;
    }

    public IPostgresRepositoryConfigurator<TSaga> SetOptimisticConcurrency(string versionColumnName = "xmin")
    {
        ConcurrencyMode = ConcurrencyMode.Optimistic;
        VersionColumnName = versionColumnName;
        return this;
    }

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

        sagaConfigurator.SetContextFactory(ConfiguredSqlServerContextFactory);
    }
    
    ISagaSqlFormatter<TSaga> ConfiguredFormatter() => ConcurrencyMode == ConcurrencyMode.Optimistic
        ? new OptimisticPostgresSagaFormatter<TSaga>(TableName, IdentityColumnName, VersionColumnName)
        : new PessimisticPostgresSagaFormatter<TSaga>(TableName, IdentityColumnName);

    ISagaSqlConnectionProvider<TSaga> ConfiguredConnectionProvider() =>
        new PostgresSqlConnectionProvider<TSaga>(ConnectionString!, ConcurrencyMode == ConcurrencyMode.Optimistic ? null : IsolationLevel);

    static Task<DatabaseContext<TSaga>> ConfiguredSqlServerContextFactory(IServiceProvider serviceProvider)
        => Task.FromResult<DatabaseContext<TSaga>>(serviceProvider.GetRequiredService<SagaDatabaseContext<TSaga>>());

    void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(ConfiguredFormatter());
        services.AddSingleton(ConfiguredConnectionProvider());
    }
}

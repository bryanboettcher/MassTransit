namespace MassTransit.Dapper.SqlServer.Configuration;

using System.Data;
using Connections;
using Dapper.Configuration;
using DapperIntegration.Saga;
using DapperIntegration.SqlBuilders;
using Formatting;
using Microsoft.Extensions.DependencyInjection;

public class SqlServerRepositoryConfigurator<TSaga> : ISqlServerRepositoryConfigurator<TSaga>, ISpecification
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

    public ISqlServerRepositoryConfigurator<TSaga> SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    public ISqlServerRepositoryConfigurator<TSaga> SetTableName(string tableName)
    {
        TableName = tableName;
        return this;
    }

    public ISqlServerRepositoryConfigurator<TSaga> SetIdentityColumnName(string identityColumnName)
    {
        IdentityColumnName = identityColumnName;
        return this;
    }

    public ISqlServerRepositoryConfigurator<TSaga> SetOptimisticConcurrency(string versionColumnName = "RowVersion")
    {
        ConcurrencyMode = ConcurrencyMode.Optimistic;
        VersionColumnName = versionColumnName;
        return this;
    }

    public ISqlServerRepositoryConfigurator<TSaga> SetPessimisticConcurrency(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        ConcurrencyMode = ConcurrencyMode.Pessimistic;
        IsolationLevel = isolationLevel;
        return this;
    }
        
    public void Configure(IDapperRepositoryConfigurator<TSaga> sagaConfigurator)
    {
        (sagaConfigurator as DapperRepositoryConfigurator<TSaga>)?
            .AddCallback(RegisterServices);

        sagaConfigurator.SetContextFactory(ConfiguredSqlServerContextFactory);
    }
    
    ISagaSqlFormatter<TSaga> ConfiguredFormatter() => ConcurrencyMode == ConcurrencyMode.Optimistic
        ? new OptimisticSqlServerSagaFormatter<TSaga>(TableName, IdentityColumnName, VersionColumnName)
        : new PessimisticSqlServerSagaFormatter<TSaga>(TableName, IdentityColumnName);

    ISagaConnectionProvider<TSaga> ConfiguredConnectionProvider() =>
        new SqlServerConnectionProvider<TSaga>(ConnectionString!, IsolationLevel);

    static Task<DatabaseContext<TSaga>> ConfiguredSqlServerContextFactory(IServiceProvider serviceProvider)
        => Task.FromResult<DatabaseContext<TSaga>>(serviceProvider.GetRequiredService<SagaDatabaseContext<TSaga>>());

    void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(ConfiguredFormatter());
        services.AddSingleton(ConfiguredConnectionProvider());
    }
}

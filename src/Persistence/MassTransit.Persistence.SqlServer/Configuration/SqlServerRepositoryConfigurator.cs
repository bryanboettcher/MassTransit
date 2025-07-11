namespace MassTransit.Persistence.SqlServer.Configuration;

using System.Data;
using System.Linq.Expressions;
using Connections;
using Integration.Saga;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Configuration;


public class SqlServerRepositoryConfigurator<TSaga> : ISqlServerRepositoryConfigurator<TSaga>, ISpecification
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
    public string VersionPropertyName { get; set; }

    /// <inheritdoc />
    public string? TableName { get; set; }

    /// <inheritdoc />
    public string IdentityColumnName { get; set; } = "CorrelationId";

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            yield return this.Failure($"{nameof(ConnectionString)} must be set");

        if (string.IsNullOrWhiteSpace(TableName))
            yield return this.Failure($"{nameof(TableName)} must be set");

        if (ConcurrencyMode == ConcurrencyMode.Optimistic && string.IsNullOrWhiteSpace(VersionColumnName))
            yield return this.Failure($"{nameof(VersionColumnName)} must be set when using Optimistic concurrency");
    }

    /// <inheritdoc />
    public ISqlServerRepositoryConfigurator<TSaga> SetConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    /// <inheritdoc />
    public ISqlServerRepositoryConfigurator<TSaga> SetTableName(string tableName)
    {
        TableName = tableName;
        return this;
    }

    /// <inheritdoc />
    public ISqlServerRepositoryConfigurator<TSaga> SetIdentityColumnName(string identityColumnName)
    {
        IdentityColumnName = identityColumnName;
        return this;
    }

    /// <inheritdoc />
    public ISqlServerRepositoryConfigurator<TSaga> SetOptimisticConcurrency<TProp>(Expression<Func<TSaga, TProp>> versionPropertySelector, string versionColumnName = "RowVersion")
    {
        ConcurrencyMode = ConcurrencyMode.Optimistic;
        VersionColumnName = versionColumnName;
        VersionPropertyName = ExtractPropertyName(versionPropertySelector);
        return this;
    }

    /// <inheritdoc />
    public ISqlServerRepositoryConfigurator<TSaga> SetPessimisticConcurrency(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
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

    Task<DatabaseContext<TSaga>> ConfiguredContextFactory(IServiceProvider serviceProvider)
    {
        return ConcurrencyMode == ConcurrencyMode.Optimistic
            ? Task.FromResult<DatabaseContext<TSaga>>(new OptimisticSqlServerDatabaseContext<TSaga>(ConnectionString, TableName, IdentityColumnName, VersionColumnName, VersionPropertyName))
            : Task.FromResult<DatabaseContext<TSaga>>(new PessimisticSqlServerDatabaseContext<TSaga>(ConnectionString, TableName, IdentityColumnName, IsolationLevel));
    }

    void RegisterServices(IServiceCollection services)
    {
    }

    static string ExtractPropertyName<TProp>(Expression<Func<TSaga, TProp>> selector)
    {
        if (selector.Body is MemberExpression memberExpression)
            return memberExpression.Member.Name;

        throw new ArgumentException("The lambda expression must be a member access expression.");
    }
}

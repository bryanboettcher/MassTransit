namespace MassTransit.Persistence.SqlServer.Connections;

using System.Linq.Expressions;
using System.Reflection;
using Integration.Saga;
using Microsoft.Data.SqlClient;


public class OptimisticSqlServerDatabaseContext<TSaga> : SqlServerDatabaseContext<TSaga>, DatabaseContext<TSaga>
    where TSaga : class, ISaga
{
    readonly string _versionColumnName;
    readonly PropertyInfo _versionProperty;

    public OptimisticSqlServerDatabaseContext(string connectionString, string tableName, string idColumnName, string versionColumnName, string versionPropertyName)
        : base(connectionString, tableName, idColumnName)
    {
        _versionColumnName = versionColumnName;
        _versionProperty = ModelType.GetProperty(versionPropertyName)
            ?? throw new InvalidOperationException($"Cannot access version property {versionPropertyName} on {ModelType.Name}");
    }
        
    protected override string BuildLoadSql()
    {
        return $"SELECT TOP 1 * FROM {TableName} WHERE [{IdColumnName}] = @correlationId";
    }

    protected override string BuildQuerySql(Expression<Func<TSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
    {
        var sqlRoot = $"SELECT * FROM {TableName}";

        var predicates = SqlExpressionVisitor.CreateFromExpression(filterExpression, Mappings);

        if (predicates.Count == 0) // good luck...
            return sqlRoot;

        var queryPredicate = BuildQueryPredicate(predicates, parameterCallback);
        return string.Concat(sqlRoot, " WHERE ", queryPredicate);
    }

    protected override string BuildInsertSql()
    {
        var properties = BuildProperties(ModelType);

        properties.Remove(IdColumnName);

        var columns = string.Join(", ", properties.Select(p => $"[{p.Key}]"));
        var values = string.Join(", ", properties.Select(p => $"@{p.Value}"));

        var sql = $"INSERT INTO {TableName} ({columns}) VALUES ({values})";

        return sql;
    }

    protected override string BuildUpdateSql()
    {
        var properties = BuildProperties(ModelType);
        properties.Remove(IdColumnName);

        var updateExpression = string.Join(", ", properties.Select(p => $"[{p.Key}] = @{p.Value}"));

        var sql = $"UPDATE {TableName} SET {updateExpression} WHERE [{IdColumnName}] = @correlationId AND [{_versionColumnName}] = @{_versionProperty.Name.ToLowerInvariant()}";

        return sql;
    }

    protected override string BuildDeleteSql()
    {
        var sql = $"DELETE FROM {TableName} WHERE [{IdColumnName}] = @correlationId AND [{_versionColumnName}] = @{_versionProperty.Name.ToLowerInvariant()}";

        return sql;
    }

    protected override ValueTask OnConnectionOpened(SqlConnection connection, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

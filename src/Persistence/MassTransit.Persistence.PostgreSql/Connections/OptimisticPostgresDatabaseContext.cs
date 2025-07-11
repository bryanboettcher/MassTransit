namespace MassTransit.Persistence.PostgreSql.Connections;

using System.Linq.Expressions;
using System.Reflection;
using Integration.Saga;
using Npgsql;


public class OptimisticPostgresDatabaseContext<TSaga> : PostgresDatabaseContext<TSaga>, DatabaseContext<TSaga>
    where TSaga : class, ISaga
{
    readonly string _versionColumnName;
    readonly PropertyInfo _versionProperty;

    public OptimisticPostgresDatabaseContext(string connectionString, string tableName, string idColumnName, string versionPropertyName)
        : base(connectionString, tableName, idColumnName)
    {
        _versionColumnName = @"xmin";
        _versionProperty = ModelType.GetProperty(versionPropertyName)
            ?? throw new InvalidOperationException($"Cannot access version property {versionPropertyName} on {ModelType.Name}");
    }

    protected override string BuildLoadSql()
    {
        return $"SELECT *, xmin AS {_versionProperty.Name.ToLowerInvariant()} FROM {TableName} WHERE {IdColumnName} = @correlationid LIMIT 1";
    }

    protected override string BuildQuerySql(Expression<Func<TSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
    {
        var sqlRoot = $"SELECT *, xmin AS {_versionProperty.Name.ToLowerInvariant()} FROM {TableName}";

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

        var columns = string.Join(", ", properties.Select(p => $"{p.Key}"));
        var values = string.Join(", ", properties.Select(p => $"@{p.Value}"));

        var sql = $"INSERT INTO {TableName} ({columns}) VALUES ({values})";

        return sql;
    }

    protected override string BuildUpdateSql()
    {
        var properties = BuildProperties(ModelType);
        properties.Remove(IdColumnName);

        var updateExpression = string.Join(", ", properties.Select(p => $"{p.Key} = @{p.Value}"));

        var sql = $"UPDATE {TableName} SET {updateExpression} WHERE {IdColumnName} = @correlationid AND {_versionColumnName} = @{_versionProperty.Name.ToLowerInvariant()}";

        return sql;
    }

    protected override string BuildDeleteSql()
    {
        var sql = $"DELETE FROM {TableName} WHERE {IdColumnName} = @correlationid AND {_versionColumnName} = @{_versionProperty.Name.ToLowerInvariant()}";

        return sql;
    }

    protected override ValueTask OnConnectionOpened(NpgsqlConnection connection, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

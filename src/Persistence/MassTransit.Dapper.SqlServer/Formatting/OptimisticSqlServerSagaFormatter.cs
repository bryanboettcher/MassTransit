namespace MassTransit.Dapper.SqlServer.Formatting;

using System.Linq.Expressions;
using Integration.Saga;
using Integration.SqlBuilders;


public class OptimisticSqlServerSagaFormatter<TModel> : SagaFormatterBase, ISagaSqlFormatter<TModel>
    where TModel : class
{
    readonly string _tableName;
    readonly string _idColumnName;
    readonly string _versionColumnName;

    public OptimisticSqlServerSagaFormatter(string? tableName = null, string? idColumnName = null, string? versionColumnName = null)
    {
        var type = typeof(TModel);

        _tableName = tableName ?? GetTableName(type);
        _idColumnName = idColumnName ?? GetIdColumnName(type);
        _versionColumnName = versionColumnName ?? GetVersionColumnName<byte[]>(type, "rowversion");

        if (_versionColumnName is null)
            throw new InvalidOperationException($"Optimistic concurrency cannot be used with {type.Name} because the ROWVERSION column was not auto-detected.  Either specify the column name directly in the constructor, or ensure a 'public byte[] RowVersion' property exists.");
    }

    public string BuildLoadSql()
    {
        return $"SELECT TOP 1 * FROM {_tableName} WHERE [{_idColumnName}] = @correlationId";
    }

    public string BuildQuerySql(Expression<Func<TModel, bool>> filterExpression, Action<string, object?> parameterCallback)
    {
        var sqlRoot = $"SELECT * FROM {_tableName}";

        var predicates = SqlExpressionVisitor.CreateFromExpression(filterExpression, Mappings);

        if (predicates.Count == 0) // good luck...
            return sqlRoot;

        var queryPredicate = BuildQueryPredicate(predicates, parameterCallback);
        return string.Concat(sqlRoot, " WHERE ", queryPredicate);
    }

    public static string BuildQueryPredicate(List<SqlPredicate> predicates, Action<string, object?> parameterCallback)
    {
        var queryPredicates = new List<string>();

        foreach (var p in predicates)
        {
            var paramName = $"value{queryPredicates.Count}";
            queryPredicates.Add($"[{p.Name}] {p.Operator} @{paramName}");
            parameterCallback?.Invoke(paramName, p.Value);
        }

        return string.Join(" AND ", queryPredicates);
    }

    public string BuildInsertSql()
    {
        var sagaType = typeof(TModel);

        var forbidden = new HashSet<string?> { _idColumnName, _versionColumnName };
        var properties = BuildProperties(sagaType, forbidden).ToList();

        properties.Insert(0, (col: _idColumnName, prop: "correlationId"));
        // properties.Insert(1, (col: _versionColumnName, prop: "rowversion"));

        var columns = string.Join(", ", properties.Select(p => $"[{p.col}]"));
        var values = string.Join(", ", properties.Select(p => $"@{p.prop}"));

        var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values})";

        return sql;
    }

    public string BuildUpdateSql()
    {
        var sagaType = typeof(TModel);

        var forbidden = new HashSet<string?> { _idColumnName, _versionColumnName };
        var properties = BuildProperties(sagaType, forbidden).ToList();

        // properties.Insert(0, (col: _idColumnName, prop: "correlationId"));
        // properties.Insert(1, (col: _versionColumnName, prop: "rowversion"));

        var updateExpression = string.Join(", ", properties.Select(p => $"[{p.col}] = @{p.prop}"));

        var sql = $"UPDATE {_tableName} SET {updateExpression} WHERE [{_idColumnName}] = @correlationId AND [{_versionColumnName}] = @rowversion";

        return sql;
    }

    public string BuildDeleteSql()
    {
        var sql = $"DELETE FROM {_tableName} WHERE [{_idColumnName}] = @correlationId AND [{_versionColumnName}] = @rowversion";

        return sql;
    }

    public void MapPrefix<TProperty>(Expression<Func<TModel, TProperty>> mappingExpression, string? prefixName = null)
        => MapCore(mappingExpression, prefixName, false);

    public void MapProperty<TProperty>(Expression<Func<TModel, TProperty>> mappingExpression, string targetName)
        => MapCore(mappingExpression, targetName, true);
}

namespace MassTransit.Dapper.MySql.Formatting;

using System.Data.Common;
using System.Linq.Expressions;
using Integration.Saga;
using Integration.SqlBuilders;


public class OptimisticMySqlSagaFormatter<TModel> : SagaFormatterBase, ISagaSqlFormatter<TModel>, IParameterCallback
    where TModel : class, ISaga
{
    readonly string _tableName;
    readonly string _idColumnName;
    readonly string _versionColumnName;

    public OptimisticMySqlSagaFormatter(string? tableName = null, string? idColumnName = null, string? versionColumnName = null)
    {
        var type = typeof(TModel);

        _tableName = tableName ?? GetTableName(type);
        _idColumnName = idColumnName ?? GetIdColumnName(type);
        _versionColumnName = versionColumnName ?? GetVersionColumnName<DateTime>(type, "RowVersion");

        if (_versionColumnName is null)
            throw new InvalidOperationException($"Optimistic concurrency cannot be used with {type.Name} because the RowVersion column was not auto-detected.  Either specify the column name directly in the constructor, or ensure a 'public DateTime RowVersion' property exists.");
    }

    public string BuildLoadSql()
    {
        return $"SELECT * FROM {_tableName} WHERE {_idColumnName} = @correlationid LIMIT 1";
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
            var paramName = p.Name.ToLowerInvariant();

            queryPredicates.Add($"{p.Name} {p.Operator} @{paramName}");
            parameterCallback?.Invoke($"@{paramName}", p.Value);
        }

        return string.Join(" AND ", queryPredicates);
    }

    public string BuildInsertSql()
    {
        var sagaType = typeof(TModel);

        var forbidden = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { _idColumnName, _versionColumnName };
        var properties = BuildProperties(sagaType, forbidden).ToList();

        properties.Insert(0, (col: _idColumnName, prop: "correlationid"));
        
        var columns = string.Join(", ", properties.Select(p => $"{p.col}"));
        var values = string.Join(", ", properties.Select(p => $"@{p.prop.ToLowerInvariant()}"));

        var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values})";

        return sql;
    }

    public string BuildUpdateSql()
    {
        var sagaType = typeof(TModel);

        var forbidden = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { _idColumnName, _versionColumnName };
        var properties = BuildProperties(sagaType, forbidden).ToList();

        var updateExpression = string.Join(", ", properties.Select(p => $"{p.col} = @{p.prop.ToLowerInvariant()}"));

        var sql = $"UPDATE {_tableName} SET {updateExpression} WHERE {_idColumnName} = @correlationid AND {_versionColumnName} = @{_versionColumnName.ToLowerInvariant()}";

        return sql;
    }

    public string BuildDeleteSql()
    {
        var sql = $"DELETE FROM {_tableName} WHERE {_idColumnName} = @correlationid AND {_versionColumnName} = @{_versionColumnName.ToLowerInvariant()}";

        return sql;
    }

    public void MapPrefix<TProperty>(Expression<Func<TModel, TProperty>> mappingExpression, string prefixName = null)
        => MapCore(mappingExpression, prefixName, false);

    public void MapProperty<TProperty>(Expression<Func<TModel, TProperty>> mappingExpression, string targetName)
        => MapCore(mappingExpression, targetName, true);

    public void Modify(DbParameterCollection parameters)
    {
    }
}

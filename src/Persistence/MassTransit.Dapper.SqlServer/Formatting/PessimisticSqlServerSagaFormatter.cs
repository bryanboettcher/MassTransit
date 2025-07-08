namespace MassTransit.Dapper.SqlServer.Formatting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Integration.Saga;
    using Integration.SqlBuilders;


    public class PessimisticSqlServerSagaFormatter<TModel> : SagaFormatterBase, ISagaSqlFormatter<TModel>
        where TModel : class
    {
        readonly string _tableName;
        readonly string _idColumnName;
        
        public PessimisticSqlServerSagaFormatter(string? tableName = null, string? idColumnName = null)
        {
            _tableName = tableName ?? GetTableName(typeof(TModel));
            _idColumnName = idColumnName ?? GetIdColumnName(typeof(TModel));
        }
        
        public string BuildLoadSql()
        {
            return $"SELECT TOP 1 * FROM {_tableName} WITH (UPDLOCK, ROWLOCK) WHERE [{_idColumnName}] = @correlationId";
        }

        public string BuildQuerySql(Expression<Func<TModel, bool>> filterExpression, Action<string, object?> parameterCallback)
        {
            var sqlRoot = $"SELECT * FROM {_tableName} WITH (UPDLOCK, ROWLOCK)";

            var predicates = SqlExpressionVisitor.CreateFromExpression(filterExpression, Mappings);
            
            if (predicates.Count == 0) // good luck...
                return sqlRoot;

            var queryPredicate = BuildQueryPredicate(predicates, parameterCallback);
            return string.Concat(sqlRoot, " WHERE ", queryPredicate);
        }

        public static string BuildQueryPredicate(List<SqlPredicate> predicates, Action<string, object?> parameterCallback)
        {
            var queryPredicates = new List<string>();
            
            foreach(var p in predicates)
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

            var forbidden = new HashSet<string?> { _idColumnName };
            var properties = BuildProperties(sagaType, forbidden).ToList();

            properties.Insert(0, (col: GetIdColumnName(sagaType), prop: "correlationId"));

            var columns = string.Join(", ", properties.Select(p => $"[{p.col}]"));
            var values = string.Join(", ", properties.Select(p => $"@{p.prop}"));

            var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values})";

            return sql;
        }

        public string BuildUpdateSql()
        {
            var sagaType = typeof(TModel);

            var forbidden = new HashSet<string?> { _idColumnName };
            var properties = BuildProperties(sagaType, forbidden).ToList();

            var updateExpression = string.Join(", ", properties.Select(p => $"[{p.col}] = @{p.prop}"));

            var sql = $"UPDATE {_tableName} SET {updateExpression} WHERE [{_idColumnName}] = @correlationId";

            return sql;
        }

        public string BuildDeleteSql()
        {
            var sql = $"DELETE FROM {_tableName} WHERE [{_idColumnName}] = @correlationId";

            return sql;
        }

        public void MapPrefix<TProperty>(Expression<Func<TModel, TProperty>> mappingExpression, string? prefixName = null)
            => MapCore(mappingExpression, prefixName, false);

        public void MapProperty<TProperty>(Expression<Func<TModel, TProperty>> mappingExpression, string targetName)
            => MapCore(mappingExpression, targetName, true);
    }
}

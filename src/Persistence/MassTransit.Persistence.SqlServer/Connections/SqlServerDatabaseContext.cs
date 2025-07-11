namespace MassTransit.Dapper.SqlServer.Connections
{
    using System.Linq.Expressions;
    using System.Runtime.CompilerServices;
    using Integration.Saga;


    public abstract class SqlServerDatabaseContext<TSaga> : SagaDatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        protected readonly string TableName;
        protected readonly string IdColumnName;

        protected SqlServerDatabaseContext(string tableName, string idColumnName)
        {
        }

        protected static string BuildQueryPredicate(List<SqlPredicate> predicates, Action<string, object?> parameterCallback)
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
    }

    public class OptimisticSqlServerDatabaseContext<TSaga> : SqlServerDatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        protected string _versionColumnName;
        
        public OptimisticSqlServerDatabaseContext(ISagaConnection<TSaga> connection)
        {
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
            var forbidden = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { IdColumnName, _versionColumnName };
            var properties = BuildProperties(ModelType, forbidden).ToList();

            properties.Insert(0, (col: IdColumnName, prop: "correlationId"));

            var columns = string.Join(", ", properties.Select(p => $"[{p.col}]"));
            var values = string.Join(", ", properties.Select(p => $"@{p.prop}"));

            var sql = $"INSERT INTO {TableName} ({columns}) VALUES ({values})";

            return sql;
        }

        protected override string BuildUpdateSql()
        {
            var forbidden = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { IdColumnName, _versionColumnName };
            var properties = BuildProperties(ModelType, forbidden).ToList();

            var updateExpression = string.Join(", ", properties.Select(p => $"[{p.col}] = @{p.prop}"));

            var sql = $"UPDATE {TableName} SET {updateExpression} WHERE [{IdColumnName}] = @correlationId AND [{_versionColumnName}] = @{_versionColumnName.ToLowerInvariant()}";

            return sql;
        }

        protected override string BuildDeleteSql()
        {
            var sql = $"DELETE FROM {TableName} WHERE [{IdColumnName}] = @correlationId AND [{_versionColumnName}] = @{_versionColumnName.ToLowerInvariant()}";

            return sql;
        }
    }

    public class PessimisticSqlServerDatabaseContext<TSaga> : SqlServerDatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        public PessimisticSqlServerDatabaseContext(ISagaConnection<TSaga> connection)
        {
        }
        
        protected override string BuildLoadSql()
        {
            return $"SELECT TOP 1 * FROM {TableName} WITH (UPDLOCK, ROWLOCK) WHERE [{IdColumnName}] = @correlationId";
        }

        protected override string BuildQuerySql(Expression<Func<TSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
        {
            var sqlRoot = $"SELECT * FROM {TableName} WITH (UPDLOCK, ROWLOCK)";

            var predicates = SqlExpressionVisitor.CreateFromExpression(filterExpression, Mappings);

            if (predicates.Count == 0) // good luck...
                return sqlRoot;

            var queryPredicate = BuildQueryPredicate(predicates, parameterCallback);
            return string.Concat(sqlRoot, " WHERE ", queryPredicate);
        }

        protected override string BuildInsertSql()
        {
            var forbidden = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { IdColumnName };
            var properties = BuildProperties(ModelType, forbidden).ToList();

            properties.Insert(0, (col: GetIdColumnName(ModelType), prop: "correlationId"));

            var columns = string.Join(", ", properties.Select(p => $"[{p.col}]"));
            var values = string.Join(", ", properties.Select(p => $"@{p.prop}"));

            var sql = $"INSERT INTO {TableName} ({columns}) VALUES ({values})";

            return sql;
        }

        protected override string BuildUpdateSql()
        {
            var forbidden = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { IdColumnName };
            var properties = BuildProperties(ModelType, forbidden).ToList();

            var updateExpression = string.Join(", ", properties.Select(p => $"[{p.col}] = @{p.prop}"));

            var sql = $"UPDATE {TableName} SET {updateExpression} WHERE [{IdColumnName}] = @correlationId";

            return sql;
        }

        protected override string BuildDeleteSql()
        {
            var sql = $"DELETE FROM {TableName} WHERE [{IdColumnName}] = @correlationId";

            return sql;
        }
    }
}

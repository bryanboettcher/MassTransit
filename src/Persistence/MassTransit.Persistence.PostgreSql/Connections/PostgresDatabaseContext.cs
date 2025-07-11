using MassTransit.Dapper.Integration.Saga;
using System.Linq.Expressions;


namespace MassTransit.Dapper.PostgreSql.Connections
{
    public abstract class PostgresDatabaseContext<TSaga> : SagaDatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        protected readonly string TableName;
        protected readonly string IdColumnName;

        protected PostgresDatabaseContext(string tableName, string idColumnName)
        {
        }

        protected static string BuildQueryPredicate(List<SqlPredicate> predicates, Action<string, object?> parameterCallback)
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
    }

    public class OptimisticPostgresDatabaseContext<TSaga> : PostgresDatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        protected string _versionColumnName;

        public OptimisticPostgresDatabaseContext(ISagaConnection<TSaga> connection)
        {
        }

        protected override string BuildLoadSql()
        {
            return $"SELECT *, xmin AS {_versionColumnName} FROM {TableName} WHERE {IdColumnName} = @correlationid LIMIT 1";
        }

        protected override string BuildQuerySql(Expression<Func<TSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
        {
            var sqlRoot = $"SELECT *, xmin AS {_versionColumnName} FROM {TableName}";

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

            properties.Insert(0, (col: IdColumnName, prop: "correlationid"));

            var columns = string.Join(", ", properties.Select(p => $"{p.col}"));
            var values = string.Join(", ", properties.Select(p => $"@{p.prop.ToLowerInvariant()}"));

            var sql = $"INSERT INTO {TableName} ({columns}) VALUES ({values})";

            return sql;
        }

        protected override string BuildUpdateSql()
        {
            var forbidden = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { IdColumnName, _versionColumnName };
            var properties = BuildProperties(ModelType, forbidden).ToList();

            var updateExpression = string.Join(", ", properties.Select(p => $"{p.col} = @{p.prop.ToLowerInvariant()}"));

            var sql = $"UPDATE {TableName} SET {updateExpression} WHERE {IdColumnName} = @correlationid AND xmin = @xmin";

            return sql;
        }

        protected override string BuildDeleteSql()
        {
            var sql = $"DELETE FROM {TableName} WHERE {IdColumnName} = @correlationid AND xmin = @xmin";

            return sql;
        }
    }

    public class PessimisticPostgresDatabaseContext<TSaga> : PostgresDatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        public PessimisticPostgresDatabaseContext(ISagaConnection<TSaga> connection)
        {
        }

        protected override string BuildLoadSql()
        {
            return $"SELECT * FROM {TableName} WHERE {IdColumnName} = @correlationid FOR UPDATE LIMIT 1";
        }

        protected override string BuildQuerySql(Expression<Func<TSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
        {
            var sqlRoot = $"SELECT * FROM {TableName}";
            var sqlLock = " FOR UPDATE";

            var predicates = SqlExpressionVisitor.CreateFromExpression(filterExpression, Mappings);

            if (predicates.Count == 0) // good luck...
                return string.Concat(sqlRoot, sqlLock);

            var queryPredicate = BuildQueryPredicate(predicates, parameterCallback);
            return string.Concat(sqlRoot, " WHERE ", queryPredicate, sqlLock);
        }

        protected override string BuildInsertSql()
        {
            var forbidden = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { IdColumnName };
            var properties = BuildProperties(ModelType, forbidden).ToList();
            properties.Insert(0, (col: IdColumnName, prop: "correlationid"));

            var columns = string.Join(", ", properties.Select(p => $"{p.col}"));
            var values = string.Join(", ", properties.Select(p => $"@{p.prop.ToLowerInvariant()}"));

            var sql = $"INSERT INTO {TableName} ({columns}) VALUES ({values})";

            return sql;
        }

        protected override string BuildUpdateSql()
        {
            var forbidden = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { IdColumnName };
            var properties = BuildProperties(ModelType, forbidden).ToList();

            var updateExpression = string.Join(", ", properties.Select(p => $"{p.col} = @{p.prop.ToLowerInvariant()}"));

            var sql = $"UPDATE {TableName} SET {updateExpression} WHERE {IdColumnName} = @correlationid";

            return sql;
        }

        protected override string BuildDeleteSql()
        {
            var sql = $"DELETE FROM {TableName} WHERE {IdColumnName} = @correlationid";

            return sql;
        }
    }
}

namespace MassTransit.Persistence.PostgreSql.Connections;

using System.Data;
using System.Linq.Expressions;
using Integration.Saga;
using Npgsql;


public class PessimisticPostgresDatabaseContext<TSaga> : PostgresDatabaseContext<TSaga>, DatabaseContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IsolationLevel _isolationLevel;

    public PessimisticPostgresDatabaseContext(string connectionString, string tableName, string idColumnName, IsolationLevel isolationLevel)
        : base(connectionString, tableName, idColumnName)
    {
        _isolationLevel = isolationLevel;
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

        var sql = $"UPDATE {TableName} SET {updateExpression} WHERE {IdColumnName} = @correlationid";

        return sql;
    }

    protected override string BuildDeleteSql()
    {
        var sql = $"DELETE FROM {TableName} WHERE {IdColumnName} = @correlationid";

        return sql;
    }

    protected override async ValueTask OnConnectionOpened(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        Transaction = await connection.BeginTransactionAsync(_isolationLevel, cancellationToken)
            .ConfigureAwait(false);
    }
}

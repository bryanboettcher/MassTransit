namespace MassTransit.Persistence.Tests.ComponentTests;

using System.Linq.Expressions;

public interface ISagaSqlFormatter
{
    string BuildLoadSql();

    string BuildQuerySql<TSaga>(Expression<Func<TSaga, bool>> filterExpression, Action<string, object?> parameterCallback);

    string BuildInsertSql();

    string BuildUpdateSql();

    string BuildDeleteSql();
}

namespace MassTransit.DapperIntegration.Saga;

using System;
using System.Linq.Expressions;
using Dapper;


public interface SqlBuilder<TModel> where TModel : class
{
    string BuildLoadSql();
    string BuildQuerySql(Expression<Func<TModel, bool>> filterExpression, out DynamicParameters parameters);
    string BuildInsertSql();
    string BuildUpdateSql();
    string BuildDeleteSql();
}

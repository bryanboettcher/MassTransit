namespace MassTransit.DapperIntegration.SqlBuilders
{
    using System;
    using System.Linq.Expressions;


    public interface SqlBuilder<TModel> where TModel : class
    {
        string BuildInsertSql();
        string BuildUpdateSql();
        string BuildDeleteSql();
        string BuildLoadSql();
        string BuildQuerySql(Expression<Func<TModel, bool>> filterExpression, Action<string, object> parameterCallback);
    }
} 

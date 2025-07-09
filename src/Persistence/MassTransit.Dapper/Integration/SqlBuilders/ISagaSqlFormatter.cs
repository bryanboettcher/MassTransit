namespace MassTransit.Dapper.Integration.SqlBuilders
{
    using System.Linq.Expressions;

    /// <summary>
    /// Used internally to prepare the SQL for saga operations.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface ISagaSqlFormatter<TModel>
        where TModel : class
    {
        /// <summary>
        /// Creates the engine-specific INSERT statement for <typeparamref name="TModel"/>
        /// </summary>
        string BuildInsertSql();

        /// <summary>
        /// Creates the engine-specific UPDATE statement for <typeparamref name="TModel"/>
        /// </summary>
        string BuildUpdateSql();

        /// <summary>
        /// Creates the engine-specific DELETE statement for <typeparamref name="TModel"/>
        /// </summary>
        string BuildDeleteSql();

        /// <summary>
        /// Creates the engine-specific SELECT statement for <typeparamref name="TModel"/>
        /// </summary>
        string BuildLoadSql();

        /// <summary>
        /// Creates the engine-specific SELECT statement for <typeparamref name="TModel"/>
        /// </summary>
        string BuildQuerySql(Expression<Func<TModel, bool>> filterExpression, Action<string, object?> parameterCallback);

        /// <summary>
        /// Used in circumstances where properties on the model don't map to columns in the database.  Prefixes
        /// are often used for joined tables in certain instances.
        /// </summary>
        void MapPrefix<TProperty>(Expression<Func<TModel, TProperty>> mappingExpression, string? prefixName = null);

        /// <summary>
        /// Used in circumstances where properties on the model don't map to columns in the database.  Prefixes
        /// are often used for joined tables in certain instances.
        /// </summary>
        void MapProperty<TProperty>(Expression<Func<TModel, TProperty>> mappingExpression, string targetName);
    }
}

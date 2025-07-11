namespace MassTransit.Persistence.Integration.Saga
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;


    /// <summary>
    /// Contains saga-specific logic as well as respecting ISagaVersion
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    public abstract class SagaDatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        protected readonly Type ModelType = typeof(TSaga);
        protected readonly List<SqlPropertyMapping> Mappings = new();
        
        protected SagaDatabaseContext()
        {
        }

        public async Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            var sql = BuildLoadSql();

            var results = ReadAsync(
                sql,
                new { correlationId },
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            // intentionally returning inside the foreach,
            // since we only need at most one result
            await foreach (var result in results)
                return result;

            return null;
        }

        public async IAsyncEnumerable<TSaga> QueryAsync(Expression<Func<TSaga, bool>> filterExpression, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object?>();
            var sql = BuildQuerySql(filterExpression, (k, v) => parameters.TryAdd(k, v));

            var results = ReadAsync(
                sql,
                parameters,
                cancellationToken
            ).ConfigureAwait(false);

            await foreach (var result in results)
                yield return result;
        }

        public async Task InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
        {
            var sql = BuildInsertSql();

            var rows = await ExecuteAsync(
                sql,
                instance,
                cancellationToken
            ).ConfigureAwait(false);

            if (rows == 0)
                throw new SagaConcurrencyException("Saga Insert failed", instance);
        }

        public async Task UpdateAsync(TSaga instance, CancellationToken cancellationToken = default)
        {
            var sql = BuildUpdateSql();

            var rows = await ExecuteAsync(
                sql,
                instance,
                cancellationToken
            ).ConfigureAwait(false);

            if (rows == 0)
                throw new SagaConcurrencyException("Saga Update failed", instance);
        }

        public async Task DeleteAsync(TSaga instance, CancellationToken cancellationToken)
        {
            var sql = BuildDeleteSql();

            var rows = await ExecuteAsync(
                sql,
                instance,
                cancellationToken
            ).ConfigureAwait(false);

            if (rows == 0)
                throw new SagaConcurrencyException("Saga Delete failed", instance);
        }

        protected abstract IAsyncEnumerable<TSaga> ReadAsync(string sql, object? parameters, CancellationToken cancellationToken);

        protected abstract Task<int> ExecuteAsync(string sql, object? parameters, CancellationToken cancellationToken);

        protected internal abstract string BuildLoadSql();

        protected internal abstract string BuildQuerySql(Expression<Func<TSaga, bool>> filterExpression, Action<string, object?> parameterCallback);

        protected internal abstract string BuildInsertSql();

        protected internal abstract string BuildUpdateSql();

        protected internal abstract string BuildDeleteSql();

        protected virtual string GetTableName(Type type)
        {
            var tableName = AttributeValue(type, "TableAttribute", "Name");
            if (!string.IsNullOrEmpty(tableName))
                return tableName;

            return type.Name + "s";
        }

        protected virtual string GetIdColumnName(Type type)
        {
            var properties = type.GetProperties();

            // support the Dapper.Contrib manual-mapping of keys or non-identity keys
            var keyColumn = AttributeValue(type, "KeyAttribute", "Name");
            if (!string.IsNullOrEmpty(keyColumn))
                return keyColumn;

            var explicitKeyColumn = AttributeValue(type, "ExplicitKeyAttribute", "Name");
            if (!string.IsNullOrEmpty(explicitKeyColumn))
                return explicitKeyColumn;

            if (properties.Any(p => p.Name == "CorrelationId"))
                return "CorrelationId";

            throw new InvalidOperationException("Only CorrelationId can be auto-detected as the key column.  Use constructor if necessary to override.");
        }

        protected virtual string? GetVersionColumnName<TProp>(Type type, string defaultName)
        {
            var candidate = type.GetProperties()
                .FirstOrDefault(p => p.Name.Equals(defaultName, StringComparison.OrdinalIgnoreCase));

            return candidate is not null && candidate.PropertyType == typeof(TProp)
                    ? candidate.Name
                    : null;
        }

        protected virtual string? GetColumnName(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            if (property is null)
                return null;

            return GetColumnName(type, property);
        }

        protected virtual string GetColumnName(Type type, PropertyInfo property)
        {
            var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
            if (columnAttribute is null || string.IsNullOrEmpty(columnAttribute.Name))
                return property.Name;

            return columnAttribute.Name;
        }

        protected virtual IDictionary<string, string> BuildProperties(Type modelType)
        {
            return (from prop in modelType.GetProperties()
                   let columnName = GetColumnName(modelType, prop)
                   let propertyName = NormalizeName(prop.Name)
                   select (columnName, propertyName))
                .DistinctBy(m => m.columnName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(m => m.columnName, m => m.propertyName, StringComparer.OrdinalIgnoreCase);
        }

        protected void MapCore<TModel, TProperty>(Expression<Func<TModel, TProperty>> mappingExpression, string? name, bool exact)
        {
            if (mappingExpression.NodeType != ExpressionType.Lambda)
                throw new InvalidOperationException("Expression must be a lambda");

            var body = mappingExpression.Body as MemberExpression;
            if (body is null)
                throw new InvalidOperationException("Expression must only be a property (x => x.Foo.Bar)");

            Mappings.Add(new()
            {
                Property = body,
                Name = name ?? body.Member.Name,
                Exact = exact,
            });
        }

        protected static string NormalizeName(string original)
        {
            return new string(original.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        }

        static string? AttributeValue(Type type, string attributeName, string propertyName)
        {
            var tableAttribute = type.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType().Name.StartsWith(attributeName, StringComparison.OrdinalIgnoreCase));

            var nameProperty = tableAttribute?.GetType().GetProperty(propertyName);
            if (nameProperty is null)
                return null;

            var nameValue = (string?)nameProperty.GetValue(tableAttribute);

            return !string.IsNullOrEmpty(nameValue)
                ? nameValue
                : null;
        }
    }
}

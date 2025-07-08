namespace MassTransit.Dapper.Integration.SqlBuilders
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq.Expressions;
    using System.Reflection;
    using Saga;


    /// <summary>
    /// Contains overridable members to help build queries
    /// </summary>
    public abstract class SagaFormatterBase
    {
        protected readonly List<SqlPropertyMapping> Mappings = new();
        
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

        protected virtual IEnumerable<(string col, string prop)> BuildProperties(Type sagaType, HashSet<string?> forbiddenColumns)
        {
            return from prop in sagaType.GetProperties()
                let columnName = GetColumnName(sagaType, prop)
                let propertyName = CamelCase(prop.Name)
                where !forbiddenColumns.Contains(columnName)
                select (columnName, propertyName);

            string CamelCase(string name)
            {
                var parts = name.Split([' ', '_']);

                // property name is something like `Name` or `CorrelationId`
                if (parts.Length == 1)
                    parts[0] = char.ToLowerInvariant(parts[0][0]) + parts[0].Substring(1);
                else
                    parts[0] = parts[0].ToLowerInvariant();

                if (parts.Length > 1)
                {
                    for (var index = 1; index < parts.Length; index++)
                    {
                        parts[index] = char.ToUpperInvariant(parts[index][0]) + parts[index].Substring(1).ToLowerInvariant();
                    }
                }

                return string.Concat(parts);
            }
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

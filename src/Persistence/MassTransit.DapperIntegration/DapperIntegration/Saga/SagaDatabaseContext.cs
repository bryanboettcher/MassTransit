namespace MassTransit.DapperIntegration.Saga;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

/// <summary>
/// Contains saga-specific logic as well as respecting ISagaVersion
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public sealed class SagaDatabaseContext<TSaga> : DatabaseContext<TSaga>, SqlBuilder<TSaga>
    where TSaga : class, ISaga
{
    readonly IDbConnection _connection;
    readonly IDbTransaction _transaction;

    readonly string _tableName;
    readonly string _idColumnName;
    readonly string _versionColumnName;
    readonly bool _isVersioned;
    
    public SagaDatabaseContext(IDbConnection connection, IDbTransaction transaction, string tableName = default, string idColumnName = default)
    {
        _connection = connection;
        _transaction = transaction;

        _tableName = tableName ?? GetTableName(typeof(TSaga));
        _idColumnName = idColumnName ?? GetIdColumnName(typeof(TSaga));
        _versionColumnName = GetColumnName(typeof(TSaga), nameof(ISagaVersion.Version));

        _isVersioned = typeof(ISagaVersion).IsAssignableFrom(typeof(TSaga));
    }
    
    public Task<TSaga> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        var param = new DynamicParameters();
        param.Add("correlationId", correlationId);

        var sql = BuildLoadSql();

        return _connection.QueryFirstOrDefaultAsync<TSaga>(sql, param, _transaction);
    }

    public Task<IEnumerable<TSaga>> QueryAsync(Expression<Func<TSaga, bool>> filterExpression, CancellationToken cancellationToken)
    {
        var sql = BuildQuerySql(filterExpression, out var parameters);
        
        return _connection.QueryAsync<TSaga>(sql, parameters, _transaction);
    }
    
    public Task InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        var sql = BuildInsertSql();

        if (instance is ISagaVersion versioned)
        {
            versioned.Version = 1;
        }

        return _connection.ExecuteAsync(sql, instance, _transaction);
    }

    public async Task UpdateAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        var sql = BuildUpdateSql();
        
        var param = new DynamicParameters();
        param.AddDynamicParams(instance);
        param.Add("correlationId", instance.CorrelationId);

        if (instance is ISagaVersion versioned)
        {
            versioned.Version++;
            param.Add("version", versioned.Version);
        }

        var rows = await _connection.ExecuteAsync(sql, param, _transaction);
        if ((rows == 0) == (instance is ISagaVersion))
        {
            throw new DapperConcurrencyException("Saga Update failed", typeof(TSaga), instance.CorrelationId);
        }
    }
    
    public async Task DeleteAsync(TSaga instance, CancellationToken cancellationToken)
    {
        var sql = BuildDeleteSql();

        var param = new DynamicParameters();
        param.Add("correlationId", instance.CorrelationId);

        if (instance is ISagaVersion versioned)
        {
            versioned.Version++;
            param.Add("version", versioned.Version);
        }

        var rows = await _connection.ExecuteAsync(sql, param, _transaction);
        if ((rows == 0) == (instance is ISagaVersion))
        {
            throw new DapperConcurrencyException("Saga Delete failed", typeof(TSaga), instance.CorrelationId);
        }
    }

    public void Commit()
    {
        _transaction?.Commit();
    }

    public ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        _transaction?.Dispose();

        return default;
    }

    static string GetTableName(Type type)
    {
        var tableAttribute = type.GetCustomAttribute<Dapper.Contrib.Extensions.TableAttribute>();
        if (tableAttribute is not null)
            return tableAttribute.Name;

        return type.Name + "s";
    }

    static string GetIdColumnName(Type type)
    {
        if (type.GetProperties().Any(p => p.Name == "CorrelationId"))
            return "CorrelationId";

        throw new InvalidOperationException("Only CorrelationId can be auto-detected as the key column.  Use constructor if necessary to override.");
    }

    static string GetColumnName(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        if (property is null)
            return propertyName;

        return GetColumnName(type, property);
    }

    static string GetColumnName(Type type, PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        if (columnAttribute is null || string.IsNullOrEmpty(columnAttribute.Name))
            return property.Name;

        return columnAttribute.Name;
    }

    IEnumerable<(string col, string prop)> BuildProperties(Type sagaType)
    {
        var forbiddenColumns = new HashSet<string> { _idColumnName };

        if (_isVersioned)
            forbiddenColumns.Add(_versionColumnName);

        return from prop in sagaType.GetProperties()
               let columnName = GetColumnName(sagaType, prop)
               let propertyName = CamelCase(prop.Name)
               where ! forbiddenColumns.Contains(columnName)
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
                for ( var index = 1; index < parts.Length; index++ )
                {
                    parts[index] = char.ToUpperInvariant(parts[index][0]) + parts[index].Substring(1).ToLowerInvariant();
                }
            }

            return string.Concat(parts);
        }
    }

    public string BuildLoadSql()
    {
        return $"SELECT * FROM {_tableName} WITH (UPDLOCK, ROWLOCK) WHERE [{_idColumnName}] = @correlationId";
    }

    public string BuildQuerySql(Expression<Func<TSaga, bool>> filterExpression, out DynamicParameters parameters)
    {
        (var whereStatement, parameters) = WhereStatementHelper.GetWhereStatementAndParametersFromExpression(filterExpression);

        return $"SELECT * FROM {_tableName} WITH (UPDLOCK, ROWLOCK) {whereStatement}";
    }

    public string BuildInsertSql()
    {
        var sagaType = typeof(TSaga);

        var properties = BuildProperties(sagaType).ToList();
        properties.Insert(0, (col: GetIdColumnName(sagaType), prop: "correlationId"));

        if (_isVersioned)
            properties.Insert(1, (col: GetColumnName(sagaType, nameof(ISagaVersion.Version)), prop: "version"));

        var columns = string.Join(", ", properties.Select(p => $"[{p.col}]"));
        var values = string.Join(", ", properties.Select(p => $"@{p.prop}"));

        var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values})";
        
        return sql;
    }

    public string BuildUpdateSql()
    {
        var properties = BuildProperties(typeof(TSaga));

        var updateExpression = string.Join(", ", properties.Select(p => $"[{p.col}] = @{p.prop}"));

        var sql = $"UPDATE {_tableName} SET {updateExpression} WHERE [{_idColumnName}] = @correlationId";

        if (_isVersioned)
            sql += $" AND [{_versionColumnName}] < @version";

        return sql;
    }

    public string BuildDeleteSql()
    {
        var sql = $"DELETE FROM {_tableName} WHERE [{_idColumnName}] = @correlationId";

        if (_isVersioned)
            sql += $" AND [{_versionColumnName}] < @version";

        return sql;
    }
}

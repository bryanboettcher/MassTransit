namespace MassTransit.DapperIntegration.Saga;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
    readonly SqlConnection _connection;
    readonly SqlTransaction _transaction;

    readonly string _tableName;
    readonly string _idColumnName;
    readonly string _versionColumnName;
    
    public SagaDatabaseContext(SqlConnection connection, SqlTransaction transaction, string tableName = default, string idColumnName = default)
    {
        _connection = connection;
        _transaction = transaction;

        _tableName = tableName ?? GetTableName(typeof(TSaga));
        _idColumnName = idColumnName ?? GetIdColumnName(typeof(TSaga));
        _versionColumnName = GetColumnName(typeof(TSaga), nameof(ISagaVersion.Version));
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


    }

    public async Task UpdateAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        var sql = BuildUpdateSql();
        
        var param = new DynamicParameters();
        param.Add("correlationId", instance.CorrelationId);

        if (instance is ISagaVersion versioned)
            param.Add("version", versioned.Version);

        var rows = await _connection.ExecuteAsync(sql, instance, _transaction);
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
            param.Add("version", versioned.Version);

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
        if (property is null) // the hell?
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

    static string BuildUpdateExpression(Type sagaType)
    {
        var expressions =
            from prop in sagaType.GetProperties()
            let columnName = GetColumnName(sagaType, prop)
            let propertyName = prop.Name
            select $"[{columnName}] = @{propertyName}";

        return string.Join(',', expressions);
    }

    string BuildLoadSql()
    {
        return $"SELECT * FROM {_tableName} WITH (UPDLOCK, ROWLOCK) WHERE {_idColumnName} = @correlationId";
    }

    string BuildQuerySql(Expression<Func<TSaga, bool>> filterExpression, out DynamicParameters parameters)
    {
        (var whereStatement, parameters) = WhereStatementHelper.GetWhereStatementAndParametersFromExpression(filterExpression);

        return $"SELECT * FROM {_tableName} WITH (UPDLOCK, ROWLOCK) {whereStatement}";
    }

    string BuildUpdateSql()
    {
        var updateExpression = BuildUpdateExpression(typeof(TSaga));

        var sql = $"UPDATE {_tableName} SET {updateExpression} WHERE {_idColumnName} = @correlationId";

        if (typeof(ISagaVersion).IsAssignableFrom(typeof(TSaga)))
            sql += $" AND {_versionColumnName} = @version";

        return sql;
    }

    string BuildDeleteSql()
    {
        var sql = $"DELETE FROM {_tableName} WHERE {_idColumnName} = @correlationId";

        if (typeof(ISagaVersion).IsAssignableFrom(typeof(TSaga)))
            sql += $" AND {_versionColumnName} = @version";

        return sql;
    }

}


public interface SqlBuilder<TModel> where TModel : class
{
}

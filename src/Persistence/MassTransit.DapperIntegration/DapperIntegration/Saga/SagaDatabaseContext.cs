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
using Internals;
using Microsoft.Data.SqlClient;

/// <summary>
/// Contains saga-specific logic as well as respecting ISagaVersion
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public sealed class SagaDatabaseContext<TSaga> : DatabaseContext<TSaga>
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
    
    public async Task DeleteAsync(TSaga instance, CancellationToken cancellationToken)
    {
        var param = new DynamicParameters();
        param.Add(_idColumnName, instance.CorrelationId);

        var sql = $"DELETE FROM {_tableName} WHERE {_idColumnName} = @id";

        if (instance is ISagaVersion versioned)
        {
            sql += $" AND {_versionColumnName} = @version";
            param.Add(_versionColumnName, versioned.Version);
        }

        var rows = await _connection.ExecuteAsync(sql, param, _transaction);

    }

    public Task<TSaga> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<TSaga>> QueryAsync(Expression<Func<TSaga, bool>> filterExpression, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Commit()
    {
        throw new NotImplementedException();
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

        var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        if (columnAttribute is null || string.IsNullOrEmpty(columnAttribute.Name))
            return propertyName;

        return columnAttribute.Name;
    }
}

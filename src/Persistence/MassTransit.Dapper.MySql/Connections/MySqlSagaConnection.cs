namespace MassTransit.Dapper.MySql.Connections;

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using global::MySql.Data.MySqlClient;
using Integration.Saga;
using Integration.SqlBuilders;


public class MySqlSagaConnection<TModel> : ISagaConnection<TModel>
    where TModel : class, ISaga
{
    readonly MySqlConnection _connection;
    readonly MySqlTransaction? _transaction;

    bool _disposed;

    public MySqlSagaConnection(MySqlConnection connection, MySqlTransaction? transaction)
    {
        _connection = connection;
        _transaction = transaction;
        _disposed = false;
    }

    public async IAsyncEnumerable<TModel> ReadAsync(
        string query,
        object? parameters = null,
        Func<IDataReader, TModel>? adapter = null,
        Action<DbParameterCollection>? parameterCallback = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        adapter ??= ReflectionsAdapter.CreateFor<TModel>();

        await using var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = query;

        if (parameters is not null)
            AssignParameters(command, parameters);

        parameterCallback?.Invoke(command.Parameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return adapter(reader);
        }
    }

    public async Task<int> RunAsync(
        string query,
        object? parameters = null,
        Action<DbParameterCollection>? parameterCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        await using var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = query;

        if (parameters is not null)
            AssignParameters(command, parameters);

        parameterCallback?.Invoke(command.Parameters);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return _transaction?.CommitAsync(cancellationToken)
            ?? Task.CompletedTask;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_transaction is not null)
            await _transaction.DisposeAsync().ConfigureAwait(false);

        await _connection.DisposeAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _transaction?.Dispose();
        _connection.Dispose();

        GC.SuppressFinalize(this);
        _disposed = true;
    }

    static void AssignParameters(MySqlCommand command, object? parameters)
    {
        foreach (var (name, value) in ParameterReader.Read(parameters))
        {
            if (value is Guid g)
            {
                command.Parameters.AddWithValue(name, g.ToByteArray());
            }
            else
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }
        }
    }

    static TModel ConvertReader(IDataReader reader)
    {
        var dataReader = (MySqlDataReader) reader;
        var model = Activator.CreateInstance<TModel>();

        var properties = typeof(TModel).GetProperties()
            .Where(p => p is { CanRead: true, CanWrite: true });

        foreach (var prop in properties)
        {
            var propertyName = prop.Name;
            var propertyType = prop.PropertyType;

            prop.SetValue(model, Read(propertyType, propertyName, dataReader));
        }

        return model;

        static object? Read(Type propertyType, string propertyName, MySqlDataReader reader)
        {
            var type = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                ? propertyType.GenericTypeArguments[0]
                : propertyType;

            if (_mappings.TryGetValue(type, out var converter))
            {
                return reader.IsDBNull(propertyName)
                    ? null
                    : converter(reader, propertyName);
            }

            return reader.IsDBNull(propertyName)
                ? null
                : reader.GetValue(propertyName);
        }
    }

    static readonly Dictionary<Type, Func<MySqlDataReader, string, object>> _mappings = new()
    {
        // Integer types
        { typeof(byte), (reader, col) => reader.GetByte(col) },
        { typeof(sbyte), (reader, col) => reader.GetSByte(col) },
        { typeof(short), (reader, col) => reader.GetInt16(col) },
        { typeof(ushort), (reader, col) => reader.GetUInt16(col) },
        { typeof(int), (reader, col) => reader.GetInt32(col) },
        { typeof(uint), (reader, col) => reader.GetUInt32(col) },
        { typeof(long), (reader, col) => reader.GetInt64(col) },
        { typeof(ulong), (reader, col) => reader.GetUInt64(col) },
    
        // Floating point types
        { typeof(float), (reader, col) => reader.GetFloat(col) },
        { typeof(double), (reader, col) => reader.GetDouble(col) },
        { typeof(decimal), (reader, col) => reader.GetDecimal(col) },
    
        // Character and string types
        { typeof(char), (reader, col) => reader.GetChar(col) },
        { typeof(string), (reader, col) => reader.GetString(col) },
    
        // Boolean type
        { typeof(bool), (reader, col) => reader.GetBoolean(col) },
    
        // Date and time types
        { typeof(DateTime), (reader, col) => reader.GetDateTime(col) },
        { typeof(TimeSpan), (reader, col) => reader.GetTimeSpan(col) },
        
        // Guid type
        { typeof(Guid), (reader, col) => reader.GetGuid(col) },
    };
}

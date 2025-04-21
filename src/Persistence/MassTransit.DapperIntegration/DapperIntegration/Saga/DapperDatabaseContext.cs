namespace MassTransit.DapperIntegration.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using Dapper.Contrib.Extensions;
    using Microsoft.Data.SqlClient;


    public class DapperDatabaseContext<TSaga> :
        DatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        readonly SqlConnection _connection;
        readonly SqlTransaction _transaction;
        readonly ISqlAdapter _adapter;

        public DapperDatabaseContext(SqlConnection connection, SqlTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
            _adapter = CreateAdapter(connection);
        }
        
        public Task InsertAsync(TSaga instance, CancellationToken cancellationToken)
        {
            AssertAttributes(instance);

            return _connection.InsertAsync(instance, _transaction);
        }

        public Task UpdateAsync(TSaga instance, CancellationToken cancellationToken)
        {
            AssertAttributes(instance);

            return _connection.UpdateAsync(instance, _transaction);
        }

        public Task DeleteAsync(TSaga instance, CancellationToken cancellationToken)
        {
            AssertAttributes(instance);

            var correlationId = instance?.CorrelationId ?? throw new ArgumentNullException(nameof(instance));
            var sql = $"DELETE FROM {GetTableName()} WHERE CorrelationId = @correlationId";

            return _connection.QueryAsync(sql, new { correlationId }, _transaction);
        }

        public Task<TSaga> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            var sql = $"SELECT * FROM {GetTableName()} WITH (UPDLOCK, ROWLOCK) WHERE CorrelationId = @correlationId";

            return _connection.QuerySingleOrDefaultAsync<TSaga>(sql, new { correlationId }, _transaction);
        }

        public Task<IEnumerable<TSaga>> QueryAsync(Expression<Func<TSaga, bool>> filterExpression, CancellationToken cancellationToken)
        {
            var tableName = GetTableName();
            var (whereStatement, parameters) = WhereStatementHelper.GetWhereStatementAndParametersFromExpression(filterExpression);
            var sql = $"SELECT * FROM {tableName} WITH (UPDLOCK, ROWLOCK) {whereStatement}";

            return _connection.QueryAsync<TSaga>(sql, parameters, _transaction);
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public ValueTask DisposeAsync()
        {
            _transaction.Dispose();
            _connection.Dispose();

            return default;
        }

        static string GetTableName()
        {
            return typeof(TSaga).GetCustomAttribute<TableAttribute>()?.Name ?? $"{typeof(TSaga).Name}s";
        }

        static HashSet<Type> ValidatedTypes { get; } = new();

        static void AssertAttributes(TSaga instance)
        {
            if (instance is not ISagaVersion version)
                return;

            var sagaType = typeof(TSaga);

            if (ValidatedTypes.Contains(sagaType))
                return;

            var versionProperty = sagaType.GetProperty(nameof(ISagaVersion.Version));
            if (versionProperty is null)
                return;

            var explicitKeyAttribute = versionProperty.GetCustomAttribute<ExplicitKeyAttribute>();
            var keyAttribute = versionProperty.GetCustomAttribute<KeyAttribute>();

            if (explicitKeyAttribute is null && keyAttribute is null)
                throw new InvalidOperationException("DapperIntegration requires the [Key] or [ExplicitKey] attribute set on the ISagaVersion.Version property");

            ValidatedTypes.Add(sagaType);
        }

        static ISqlAdapter CreateAdapter(SqlConnection connection)
        {
            
        }
    }

    public class DefaultSqlServerAdapter : ISqlAdapter
    {
        public async Task<int> InsertAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            int? commandTimeout,
            string tableName,
            string columnList,
            string parameterList,
            IEnumerable<PropertyInfo> keyProperties,
            object entityToInsert
        )
        {
            var cmd = $"INSERT INTO {tableName} ({columnList}) OUTPUT INSERTED.* VALUES ({parameterList});";
            var multi = await connection.QueryMultipleAsync(cmd, entityToInsert, transaction, commandTimeout).ConfigureAwait(false);

            var first = await multi.ReadFirstOrDefaultAsync().ConfigureAwait(false);
            if (first == null || first.id == null) return 0;

            var id = (int)first.id;
            var pi = keyProperties as PropertyInfo[] ?? keyProperties.ToArray();
            if (pi.Length == 0) return id;

            var idp = pi[0];
            idp.SetValue(entityToInsert, Convert.ChangeType(id, idp.PropertyType), null);

            return id;
        }

        public int Insert(
            IDbConnection connection,
            IDbTransaction transaction,
            int? commandTimeout,
            string tableName,
            string columnList,
            string parameterList,
            IEnumerable<PropertyInfo> keyProperties,
            object entityToInsert
        )
        {
            throw new NotImplementedException();
        }

        public void AppendColumnName(StringBuilder sb, string columnName)
        {
            throw new NotImplementedException();
        }

        public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
        {
            throw new NotImplementedException();
        }
    }

    public class VersionedSqlServerAdapter : ISqlAdapter { }
}

namespace MassTransit.DapperIntegration.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
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

        public DapperDatabaseContext(SqlConnection connection, SqlTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }
        
        public Task InsertAsync(TSaga instance, CancellationToken cancellationToken)
        {
            return _connection.InsertAsync(instance, _transaction);
        }

        public Task UpdateAsync(TSaga instance, CancellationToken cancellationToken)
        {
            return _connection.UpdateAsync(instance, _transaction);
        }

        public Task DeleteAsync(TSaga instance, CancellationToken cancellationToken)
        {
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

#if NETFRAMEWORK || NETSTANDARD2_0
        public Task CommitAsync(CancellationToken token = default)
        {
            _transaction.Commit();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _transaction.Dispose();
            _connection.Dispose();

            return default;
        }
#else
        public Task CommitAsync(CancellationToken token = default) => _transaction.CommitAsync(token);

        public async ValueTask DisposeAsync()
        {
            await _transaction.DisposeAsync();
            await _connection.DisposeAsync();
        }
#endif
        
        static string GetTableName()
        {
            return typeof(TSaga).GetCustomAttribute<TableAttribute>()?.Name ?? $"{typeof(TSaga).Name}s";
        }
    }
}

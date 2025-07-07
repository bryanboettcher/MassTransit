namespace MassTransit.DapperIntegration.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using SqlBuilders;


    /// <summary>
    /// Contains saga-specific logic as well as respecting ISagaVersion
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    public class SagaDatabaseContext<TSaga> : DatabaseContext<TSaga>
        where TSaga : class, ISaga
    {
        readonly ISagaSqlConnection<TSaga> _connection;
        readonly ISagaSqlFormatter<TSaga> _formatter;

        public SagaDatabaseContext(ISagaSqlConnection<TSaga> connection, ISagaSqlFormatter<TSaga> formatter)
        {
            _connection = connection;
            _formatter = formatter;
        }
    
        public async Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            var results = _connection.ReadAsync(
                _formatter.BuildLoadSql(),
                new { correlationId },
                null,
                cancellationToken
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
            var sql = _formatter.BuildQuerySql(filterExpression, (k, v) => parameters.TryAdd(k, v));

            var results = _connection.ReadAsync(
                sql,
                parameters,
                null,
                cancellationToken
            ).ConfigureAwait(false);

            await foreach (var result in results)
                yield return result;
        }
    
        public async Task InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
        {
            var sql = _formatter.BuildInsertSql();

            var rows = await ExecuteSql(
                sql,
                instance,
                cancellationToken
            ).ConfigureAwait(false);

            if (rows == 0)
                throw new SagaConcurrencyException("Saga Insert failed", instance);
        }

        public async Task UpdateAsync(TSaga instance, CancellationToken cancellationToken = default)
        {
            var sql = _formatter.BuildUpdateSql();
        
            var rows = await ExecuteSql(
                sql,
                instance,
                cancellationToken
            ).ConfigureAwait(false);

            if (rows == 0)
                throw new SagaConcurrencyException("Saga Update failed", instance);
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return _connection.CommitAsync(cancellationToken);
        }

        public async Task DeleteAsync(TSaga instance, CancellationToken cancellationToken)
        {
            var sql = _formatter.BuildDeleteSql();

            var rows = await ExecuteSql(
                sql,
                new { instance.CorrelationId },
                cancellationToken
            ).ConfigureAwait(false);

            if (rows == 0)
                throw new SagaConcurrencyException("Saga Delete failed", instance);
        }

        public void Dispose() => _connection.Dispose();

        public ValueTask DisposeAsync() => _connection.DisposeAsync();

        async Task<int> ExecuteSql(string sql, object parameters, CancellationToken cancellationToken)
        {
            var effected = await _connection.RunAsync(
                sql,
                parameters,
                cancellationToken
            ).ConfigureAwait(false);

            return effected;
        }
    }
}

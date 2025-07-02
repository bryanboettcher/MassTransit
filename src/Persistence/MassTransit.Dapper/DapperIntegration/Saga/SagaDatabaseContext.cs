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
    public class SagaDatabaseContext<TSaga> : DatabaseContext<TSaga>,
        IDisposable
        where TSaga : class
    {
        readonly ISagaConnectionProvider<TSaga> _connectionProvider;
        readonly ISagaSqlFormatter<TSaga> _sqlFormatter;

        public SagaDatabaseContext(ISagaConnectionProvider<TSaga> connectionProvider, ISagaSqlFormatter<TSaga> sqlFormatter)
        {
            _connectionProvider = connectionProvider;
            _sqlFormatter = sqlFormatter;
        }
    
        public async Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionProvider.CreateConnection(cancellationToken)
                .ConfigureAwait(false);

            var results = connection.ReadAsync(
                _sqlFormatter.BuildLoadSql(),
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
            await using var connection = await _connectionProvider.CreateConnection(cancellationToken)
                .ConfigureAwait(false);

            var parameters = new Dictionary<string, object?>();
            var sql = _sqlFormatter.BuildQuerySql(filterExpression, (k, v) => parameters.TryAdd(k, v));

            var results = connection.ReadAsync(
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
            var sql = _sqlFormatter.BuildInsertSql();

            var rows = await ExecuteSql(
                sql,
                instance,
                cancellationToken
            ).ConfigureAwait(false);

            if (rows == 0)
                throw new SagaConcurrencyException("Saga Insert failed", typeof(TSaga), instance.CorrelationId);
        }

        public async Task UpdateAsync(TSaga instance, CancellationToken cancellationToken = default)
        {
            var sql = _sqlFormatter.BuildUpdateSql();
        
            var rows = await ExecuteSql(
                sql,
                instance,
                cancellationToken
            ).ConfigureAwait(false);

            if (rows == 0)
                throw new SagaConcurrencyException("Saga Update failed", typeof(TSaga), instance.CorrelationId);
        }

        public async Task DeleteAsync(TSaga instance, CancellationToken cancellationToken)
        {
            var sql = _sqlFormatter.BuildDeleteSql();

            var rows = await ExecuteSql(
                sql,
                new { instance.CorrelationId },
                cancellationToken
            ).ConfigureAwait(false);

            if (rows == 0)
                throw new SagaConcurrencyException("Saga Delete failed", typeof(TSaga), instance.CorrelationId);
        }

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        async Task<int> ExecuteSql(string sql, object parameters, CancellationToken cancellationToken)
        {
            await using var connection = await _connectionProvider.CreateConnection(cancellationToken)
                .ConfigureAwait(false);

            var effected = await connection.RunAsync(
                sql,
                parameters,
                cancellationToken
            ).ConfigureAwait(false);

            return effected;
        }
    }
}

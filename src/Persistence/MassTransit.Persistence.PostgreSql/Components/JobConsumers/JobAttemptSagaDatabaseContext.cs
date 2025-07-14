namespace MassTransit.Persistence.PostgreSql.Components.JobConsumers;

using System.Data;
using System.Linq.Expressions;
using Connections;

using Npgsql;
using NpgsqlTypes;
using System;
using Extensions;


public class JobAttemptSagaDatabaseContext : PessimisticPostgresDatabaseContext<JobAttemptSaga>
{
    public JobAttemptSagaDatabaseContext(string connectionString, IsolationLevel isolationLevel)
        : base(connectionString, "JobAttempts", nameof(ISaga.CorrelationId), isolationLevel)
    {
    }

    protected override string BuildInsertSql()
    {
        return @$"""
INSERT INTO {TableName} 
    (""CorrelationId"", ""CurrentState"", ""JobId"", ""Started"", ""Faulted"",
    ""StatusCheckTokenId"", ""RetryAttempt"", ""ServiceAddress"", ""InstanceAddress"") 
VALUES
    (@correlationid, @currentstate, @jobid, @started, @faulted,
    @statuschecktokenid, @retryattempt, @serviceaddress, @instanceaddress);
""";
    }

    protected override string BuildUpdateSql()
    {
        return $@"""
UPDATE {TableName}
SET
    ""CurrentState"" = @currentstate,
    ""JobId"" = @jobid,
    ""Started"" = @started,
    ""Faulted"" = @faulted,
    ""StatusCheckTokenId"" = @statuschecktokenid,
    ""RetryAttempt"" = @retryattempt,
    ""ServiceAddress"" = @serviceaddress,
    ""InstanceAddress"" = @instanceaddress
WHERE
    ""CorrelationId"" = @correlationid;
""";
    }

    protected override string BuildDeleteSql()
    {
        return $@"DELETE FROM {TableName} WHERE ""CorrelationId"" = @correlationid;";
    }

    protected override string BuildLoadSql()
    {
        return $@"SELECT * FROM {TableName} WHERE ""CorrelationId"" = @correlationid FOR UPDATE;";
    }

    protected override string BuildQuerySql(Expression<Func<JobAttemptSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
    {
        throw new NotSupportedException("JobConsumers do not support querying");
    }

    protected override Func<IDataReader, JobAttemptSaga> CreateReaderAdapter()
        => ConvertFrom;

    static JobAttemptSaga ConvertFrom(IDataReader dataReader)
    {
        if (dataReader is not NpgsqlDataReader reader)
            throw new NotSupportedException("ConvertFrom only supports NpgsqlDataReader");

        return new JobAttemptSaga
        {
            CorrelationId = reader.GetGuid("CorrelationId"),
            CurrentState = reader.GetInt32("CurrentState"),
            JobId = reader.GetGuid("JobId"),
            Started = reader.GetDateTimeOrNull("Started"),
            Faulted = reader.GetDateTimeOrNull("Faulted"),
            StatusCheckTokenId = reader.GetGuidOrNull("StatusCheckTokenId"),
            RetryAttempt = reader.GetInt32("RetryAttempt"),
            ServiceAddress = reader.GetUri("ServiceAddress"),
            InstanceAddress = reader.GetUri("InstanceAddress")
        };
    }

    protected override Action<object?, NpgsqlParameterCollection> CreateWriterAdapter()
        => ConvertTo;

    static void ConvertTo(object? source, NpgsqlParameterCollection collection)
    {
        if (source is not JobAttemptSaga instance)
            throw new NotSupportedException("ConvertTo only supports JobAttemptSagas");

        collection.Add("@correlationId", NpgsqlDbType.Uuid).Value = instance.CorrelationId;
        collection.Add("@currentState", NpgsqlDbType.Integer).Value = instance.CurrentState;
        collection.Add("@jobId", NpgsqlDbType.Uuid).Value = instance.JobId;
        collection.Add("@started", NpgsqlDbType.Timestamp).Value = instance.Started.OrDbNull();
        collection.Add("@faulted", NpgsqlDbType.Timestamp).Value = instance.Faulted.OrDbNull();
        collection.Add("@statusCheckTokenId", NpgsqlDbType.Uuid).Value = instance.StatusCheckTokenId.OrDbNull();
        collection.Add("@retryAttempt", NpgsqlDbType.Integer).Value = instance.RetryAttempt;
        collection.Add("@serviceAddress", NpgsqlDbType.Varchar, 1000).Value = instance.ServiceAddress?.ToString().OrDbNull();
        collection.Add("@instanceAddress", NpgsqlDbType.Varchar, 1000).Value = instance.InstanceAddress?.ToString().OrDbNull();
    }
}

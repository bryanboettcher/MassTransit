namespace MassTransit.Persistence.MySql.Components.JobConsumers;

using System.Data;
using System.Linq.Expressions;
using Connections;
using Extensions;
using global::MySql.Data.MySqlClient;


public class JobAttemptSagaDatabaseContext : PessimisticMySqlDatabaseContext<JobAttemptSaga>
{
    public JobAttemptSagaDatabaseContext(string connectionString, IsolationLevel isolationLevel)
        : base(connectionString, "JobAttempts", nameof(ISaga.CorrelationId), isolationLevel)
    {
    }

    protected override string BuildInsertSql()
    {
        return @$"""
INSERT INTO {TableName} 
    (`CorrelationId`, `CurrentState`, `JobId`, `Started`, `Faulted`, `StatusCheckTokenId`, `RetryAttempt`, `ServiceAddress`, `InstanceAddress`) 
VALUES
    (@correlationid, @currentstate, @jobid, @started, @faulted, @statuschecktokenid, @retryattempt, @serviceaddress, @instanceaddress);
""";
    }

    protected override string BuildUpdateSql()
    {
        return $@"""
UPDATE {TableName}
SET
	`CurrentState` = @currentstate,
	`JobId` = @jobid,
	`Started` = @started,
	`Faulted` = @faulted,
	`StatusCheckTokenId` = @statuschecktokenid,
	`RetryAttempt` = @retryattempt,
	`ServiceAddress` = @serviceaddress,
	`InstanceAddress` = @instanceaddress
WHERE
    `CorrelationId` = @correlationid;
""";
    }

    protected override string BuildDeleteSql()
    {
        return $@"DELETE FROM {TableName} WHERE `CorrelationId` = @correlationid;";
    }

    protected override string BuildLoadSql()
    {
        return $@"SELECT * FROM {TableName} WHERE `CorrelationId` = @correlationid FOR UPDATE;";
    }

    protected override string BuildQuerySql(Expression<Func<JobAttemptSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
    {
        throw new NotSupportedException("JobConsumers do not support querying");
    }

    protected override Func<IDataReader, JobAttemptSaga> CreateReaderAdapter()
        => ConvertFrom;

    static JobAttemptSaga ConvertFrom(IDataReader dataReader)
    {
        if (dataReader is not MySqlDataReader reader)
            throw new NotSupportedException("ConvertFrom only supports MySqlDataReader");

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

    protected override Action<object?, MySqlParameterCollection> CreateWriterAdapter()
        => ConvertTo;

    static void ConvertTo(object? source, MySqlParameterCollection collection)
    {
        if (source is not JobAttemptSaga instance)
            throw new NotSupportedException("ConvertTo only supports JobAttemptSagas");

        collection.Add("@correlationId", MySqlDbType.Guid).Value = instance.CorrelationId;
        collection.Add("@currentState", MySqlDbType.Int32).Value = instance.CurrentState;
        collection.Add("@jobId", MySqlDbType.Guid).Value = instance.JobId;
        collection.Add("@started", MySqlDbType.DateTime).Value = instance.Started.OrDbNull();
        collection.Add("@faulted", MySqlDbType.DateTime).Value = instance.Faulted.OrDbNull();
        collection.Add("@statusCheckTokenId", MySqlDbType.Guid).Value = instance.StatusCheckTokenId.OrDbNull();
        collection.Add("@retryAttempt", MySqlDbType.Int32).Value = instance.RetryAttempt;
        collection.Add("@serviceAddress", MySqlDbType.VarChar, 1000).Value = instance.ServiceAddress?.ToString().OrDbNull();
        collection.Add("@instanceAddress", MySqlDbType.VarChar, 1000).Value = instance.InstanceAddress?.ToString().OrDbNull();
    }
}
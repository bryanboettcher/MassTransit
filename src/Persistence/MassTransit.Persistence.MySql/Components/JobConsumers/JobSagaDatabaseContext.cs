namespace MassTransit.Persistence.MySql.Components.JobConsumers;

using System.Data;
using System.Linq.Expressions;
using Connections;
using Extensions;
using global::MySql.Data.MySqlClient;


public class JobSagaDatabaseContext : PessimisticMySqlDatabaseContext<JobSaga>
{
    public JobSagaDatabaseContext(string connectionString, IsolationLevel isolationLevel)
        : base(connectionString, "Jobs", nameof(ISaga.CorrelationId), isolationLevel)
    {
    }

    protected override string BuildInsertSql()
    {
        return @$"""
INSERT INTO {TableName} 
    (`CorrelationId`, `CurrentState`, `Completed`, `Faulted`, `Started`, `Submitted`, `EndDate`, `NextStartDate`, `StartDate`, `AttemptId`, `JobTypeId`, `JobRetryDelayToken`, `JobSlotWaitToken`, `RetryAttempt`, `LastProgressLimit`, `LastProgressSequenceNumber`, `LastProgressValue`, `CronExpression`, `Reason`, `TimeZoneId`, `Duration`, `JobTimeout`, `ServiceAddress`, `IncompleteAttempts`, `Job`, `JobProperties`, `JobState`) 
VALUES
    (@correlationid, @currentstate, @completed, @faulted, @started, @submitted, @enddate, @nextstartdate, @startdate, @attemptid, @jobtypeid, @jobretrydelaytoken, @jobslotwaittoken, @retryattempt, @lastprogresslimit, @lastprogresssequencenumber, @lastprogressvalue, @cronexpression, @reason, @timezoneid, @duration, @jobtimeout, @serviceaddress, @incompleteattempts, @job, @jobproperties, @jobstate);
""";
    }

    protected override string BuildUpdateSql()
    {
        return $@"""
UPDATE {TableName}
SET
	`CurrentState` = @currentstate,
	`Completed` = @completed,
	`Faulted` = @faulted,
	`Started` = @started,
	`Submitted` = @submitted,
	`EndDate` = @enddate,
	`NextStartDate` = @nextstartdate,
	`StartDate` = @startdate,
	`AttemptId` = @attemptid,
	`JobTypeId` = @jobtypeid,
	`JobRetryDelayToken` = @jobretrydelaytoken,
	`JobSlotWaitToken` = @jobslotwaittoken,
	`RetryAttempt` = @retryattempt,
	`LastProgressLimit` = @lastprogresslimit,
	`LastProgressSequenceNumber` = @lastprogresssequencenumber,
	`LastProgressValue` = @lastprogressvalue,
	`CronExpression` = @cronexpression,
	`Reason` = @reason,
	`TimeZoneId` = @timezoneid,
	`Duration` = @duration,
	`JobTimeout` = @jobtimeout,
	`ServiceAddress` = @serviceaddress,
	`IncompleteAttempts` = @incompleteattempts,
	`Job` = @job,
	`JobProperties` = @jobproperties,
	`JobState` = @jobstate
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

    protected override string BuildQuerySql(Expression<Func<JobSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
    {
        throw new NotSupportedException("JobConsumers do not support querying");
    }

    protected override Func<IDataReader, JobSaga> CreateReaderAdapter()
        => ConvertFrom;

    static JobSaga ConvertFrom(IDataReader dataReader)
    {
        if (dataReader is not MySqlDataReader reader)
            throw new NotSupportedException("ConvertFrom only supports MySqlDataReader");

        return new JobSaga
        {
            CorrelationId = reader.GetGuid("CorrelationId"),
            CurrentState = reader.GetInt32("CurrentState"),
            Completed = reader.GetDateTimeOrNull("Completed"),
            Faulted = reader.GetDateTimeOrNull("Faulted"),
            Started = reader.GetDateTimeOrNull("Started"),
            Submitted = reader.GetDateTimeOrNull("Submitted"),
            EndDate = reader.GetDateTimeOffsetOrNull("EndDate"),
            NextStartDate = reader.GetDateTimeOffsetOrNull("NextStartDate"),
            StartDate = reader.GetDateTimeOffsetOrNull("StartDate"),
            AttemptId = reader.GetGuid("AttemptId"),
            JobTypeId = reader.GetGuid("JobTypeId"),
            JobRetryDelayToken = reader.GetGuidOrNull("JobRetryDelayToken"),
            JobSlotWaitToken = reader.GetGuidOrNull("JobSlotWaitToken"),
            RetryAttempt = reader.GetInt32("RetryAttempt"),
            LastProgressLimit = reader.GetInt64OrNull("LastProgressLimit"),
            LastProgressSequenceNumber = reader.GetInt64OrNull("LastProgressSequenceNumber"),
            LastProgressValue = reader.GetInt64OrNull("LastProgressValue"),
            CronExpression = reader.GetStringOrNull("CronExpression"),
            Reason = reader.GetStringOrNull("Reason"),
            TimeZoneId = reader.GetStringOrNull("TimeZoneId"),
            Duration = reader.GetTimeSpanOrNull("Duration"),
            JobTimeout = reader.GetTimeSpanOrNull("JobTimeout"),
            ServiceAddress = reader.GetUri("ServiceAddress"),
            IncompleteAttempts = reader.FromJson<List<Guid>>("IncompleteAttempts") ?? new List<Guid>(),
            Job = reader.FromJson<Dictionary<string, object>>("Job") ?? new Dictionary<string, object>(),
            JobProperties = reader.FromJson<Dictionary<string, object>>("JobProperties") ?? new Dictionary<string, object>(),
            JobState = reader.FromJson<Dictionary<string, object>>("JobState") ?? new Dictionary<string, object>()
        };
    }

    protected override Action<object?, MySqlParameterCollection> CreateWriterAdapter()
        => ConvertTo;

    static void ConvertTo(object? source, MySqlParameterCollection collection)
    {
        if (source is not JobSaga instance)
            throw new NotSupportedException("ConvertTo only supports JobSaga");

        collection.Add("@correlationId", MySqlDbType.Guid).Value = instance.CorrelationId;
        collection.Add("@currentState", MySqlDbType.Int32).Value = instance.CurrentState;
        collection.Add("@completed", MySqlDbType.DateTime).Value = instance.Completed.OrDbNull();
        collection.Add("@faulted", MySqlDbType.DateTime).Value = instance.Faulted.OrDbNull();
        collection.Add("@started", MySqlDbType.DateTime).Value = instance.Started.OrDbNull();
        collection.Add("@submitted", MySqlDbType.DateTime).Value = instance.Submitted.OrDbNull();
        collection.Add("@endDate", MySqlDbType.DateTime).Value = instance.EndDate.OrDbNull();
        collection.Add("@nextStartDate", MySqlDbType.DateTime).Value = instance.NextStartDate.OrDbNull();
        collection.Add("@startDate", MySqlDbType.DateTime).Value = instance.StartDate.OrDbNull();
        collection.Add("@attemptId", MySqlDbType.Guid).Value = instance.AttemptId;
        collection.Add("@jobTypeId", MySqlDbType.Guid).Value = instance.JobTypeId;
        collection.Add("@jobRetryDelayToken", MySqlDbType.Guid).Value = instance.JobRetryDelayToken.OrDbNull();
        collection.Add("@jobSlotWaitToken", MySqlDbType.Guid).Value = instance.JobSlotWaitToken.OrDbNull();
        collection.Add("@retryAttempt", MySqlDbType.Int32).Value = instance.RetryAttempt;
        collection.Add("@lastProgressLimit", MySqlDbType.Int64).Value = instance.LastProgressLimit.OrDbNull();
        collection.Add("@lastProgressSequenceNumber", MySqlDbType.Int64).Value = instance.LastProgressSequenceNumber.OrDbNull();
        collection.Add("@lastProgressValue", MySqlDbType.Int64).Value = instance.LastProgressValue.OrDbNull();
        collection.Add("@cronExpression", MySqlDbType.VarChar, 255).Value = instance.CronExpression.OrDbNull();
        collection.Add("@reason", MySqlDbType.Text).Value = instance.Reason.OrDbNull();
        collection.Add("@timeZoneId", MySqlDbType.VarChar, 100).Value = instance.TimeZoneId.OrDbNull();
        collection.Add("@duration", MySqlDbType.Time).Value = instance.Duration.OrDbNull();
        collection.Add("@jobTimeout", MySqlDbType.Time).Value = instance.JobTimeout.OrDbNull();
        collection.Add("@serviceAddress", MySqlDbType.VarChar, 1000).Value = instance.ServiceAddress?.ToString().OrDbNull();
        collection.Add("@incompleteAttempts", MySqlDbType.Text).Value = instance.IncompleteAttempts.ToJson().OrDbNull();
        collection.Add("@job", MySqlDbType.Text).Value = instance.Job.ToJson().OrDbNull();
        collection.Add("@jobProperties", MySqlDbType.Text).Value = instance.JobProperties.ToJson().OrDbNull();
        collection.Add("@jobState", MySqlDbType.Text).Value = instance.JobState.ToJson().OrDbNull();
    }
}
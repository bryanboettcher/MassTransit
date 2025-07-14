namespace MassTransit.Persistence.PostgreSql.Components.JobConsumers;

using System.Data;
using System.Linq.Expressions;
using Connections;

using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using Extensions;


public class JobSagaDatabaseContext : PessimisticPostgresDatabaseContext<JobSaga>
{
    public JobSagaDatabaseContext(string connectionString, IsolationLevel isolationLevel)
        : base(connectionString, "Jobs", nameof(ISaga.CorrelationId), isolationLevel)
    {
    }

    protected override string BuildInsertSql()
    {
        return @$"""
INSERT INTO {TableName} 
    (""CorrelationId"", ""CurrentState"", ""Completed"", ""Faulted"", ""Started"", ""Submitted"",
    ""EndDate"", ""NextStartDate"", ""StartDate"", ""AttemptId"", ""JobTypeId"", ""JobRetryDelayToken"",
    ""JobSlotWaitToken"", ""RetryAttempt"", ""LastProgressLimit"", ""LastProgressSequenceNumber"",
    ""LastProgressValue"", ""CronExpression"", ""Reason"", ""TimeZoneId"", ""Duration"", ""JobTimeout"",
    ""ServiceAddress"", ""IncompleteAttempts"", ""Job"", ""JobProperties"", ""JobState"") 
VALUES
    (@correlationid, @currentstate, @completed, @faulted, @started, @submitted,
    @enddate, @nextstartdate, @startdate, @attemptid, @jobtypeid, @jobretrydelaytoken,
    @jobslotwaittoken, @retryattempt, @lastprogresslimit, @lastprogresssequencenumber,
    @lastprogressvalue, @cronexpression, @reason, @timezoneid, @duration, @jobtimeout,
    @serviceaddress, @incompleteattempts, @job, @jobproperties, @jobstate);
""";
    }

    protected override string BuildUpdateSql()
    {
        return $@"""
UPDATE {TableName}
SET
	""CurrentState"" = @currentstate,
    ""Completed"" = @completed,
	""Faulted"" = @faulted,
	""Started"" = @started,
	""Submitted"" = @submitted,
	""EndDate"" = @enddate,
	""NextStartDate"" = @nextstartdate,
	""StartDate"" = @startdate,
	""AttemptId"" = @attemptid,
	""JobTypeId"" = @jobtypeid,
	""JobRetryDelayToken"" = @jobretrydelaytoken,
	""JobSlotWaitToken"" = @jobslotwaittoken,
	""RetryAttempt"" = @retryattempt,
	""LastProgressLimit"" = @lastprogresslimit,
	""LastProgressSequenceNumber"" = @lastprogresssequencenumber,
	""LastProgressValue"" = @lastprogressvalue,
	""CronExpression"" = @cronexpression,
	""Reason"" = @reason,
	""TimeZoneId"" = @timezoneid,
	""Duration"" = @duration,
	""JobTimeout"" = @jobtimeout,
	""ServiceAddress"" = @serviceaddress,
	""IncompleteAttempts"" = @incompleteattempts,
	""Job"" = @job,
	""JobProperties"" = @jobproperties,
	""JobState"" = @jobstate
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

    protected override string BuildQuerySql(Expression<Func<JobSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
    {
        throw new NotSupportedException("JobConsumers do not support querying");
    }

    protected override Func<IDataReader, JobSaga> CreateReaderAdapter()
        => ConvertFrom;

    static JobSaga ConvertFrom(IDataReader dataReader)
    {
        if (dataReader is not NpgsqlDataReader reader)
            throw new NotSupportedException("ConvertFrom only supports NpgsqlDataReader");

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

    protected override Action<object?, NpgsqlParameterCollection> CreateWriterAdapter()
        => ConvertTo;

    static void ConvertTo(object? source, NpgsqlParameterCollection collection)
    {
        if (source is not JobSaga instance)
            throw new NotSupportedException("ConvertTo only supports JobSaga");

        collection.Add("@correlationId", NpgsqlDbType.Uuid).Value = instance.CorrelationId;
        collection.Add("@currentState", NpgsqlDbType.Integer).Value = instance.CurrentState;
        collection.Add("@completed", NpgsqlDbType.Timestamp).Value = instance.Completed.OrDbNull();
        collection.Add("@faulted", NpgsqlDbType.Timestamp).Value = instance.Faulted;
        collection.Add("@started", NpgsqlDbType.Timestamp).Value = instance.Started;
        collection.Add("@submitted", NpgsqlDbType.Timestamp).Value = instance.Submitted;
        collection.Add("@endDate", NpgsqlDbType.TimestampTz).Value = instance.EndDate;
        collection.Add("@nextStartDate", NpgsqlDbType.TimestampTz).Value = instance.NextStartDate;
        collection.Add("@startDate", NpgsqlDbType.TimestampTz).Value = instance.StartDate;
        collection.Add("@attemptId", NpgsqlDbType.Uuid).Value = instance.AttemptId;
        collection.Add("@jobTypeId", NpgsqlDbType.Uuid).Value = instance.JobTypeId;
        collection.Add("@jobRetryDelayToken", NpgsqlDbType.Uuid).Value = instance.JobRetryDelayToken;
        collection.Add("@jobSlotWaitToken", NpgsqlDbType.Uuid).Value = instance.JobSlotWaitToken;
        collection.Add("@retryAttempt", NpgsqlDbType.Integer).Value = instance.RetryAttempt;
        collection.Add("@lastProgressLimit", NpgsqlDbType.Bigint).Value = instance.LastProgressLimit;
        collection.Add("@lastProgressSequenceNumber", NpgsqlDbType.Bigint).Value = instance.LastProgressSequenceNumber;
        collection.Add("@lastProgressValue", NpgsqlDbType.Bigint).Value = instance.LastProgressValue;
        collection.Add("@cronExpression", NpgsqlDbType.Varchar, 255).Value = instance.CronExpression;
        collection.Add("@reason", NpgsqlDbType.Text).Value = instance.Reason;
        collection.Add("@timeZoneId", NpgsqlDbType.Varchar, 100).Value = instance.TimeZoneId;
        collection.Add("@duration", NpgsqlDbType.Interval).Value = instance.Duration;
        collection.Add("@jobTimeout", NpgsqlDbType.Interval).Value = instance.JobTimeout;
        collection.Add("@serviceAddress", NpgsqlDbType.Varchar, 1000).Value = instance.ServiceAddress?.ToString();
        collection.Add("@incompleteAttempts", NpgsqlDbType.Jsonb).Value = instance.IncompleteAttempts.ToJson();
        collection.Add("@job", NpgsqlDbType.Jsonb).Value = instance.Job.ToJson();
        collection.Add("@jobProperties", NpgsqlDbType.Jsonb).Value = instance.JobProperties.ToJson();
        collection.Add("@jobState", NpgsqlDbType.Jsonb).Value = instance.JobState.ToJson();
    }
}

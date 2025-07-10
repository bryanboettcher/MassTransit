namespace MassTransit.Dapper.Integration.SqlBuilders;

using System.Data.Common;


/// <summary>
/// Used internally to allow cross-domain concerns between a SQL builder and how parameters should behave.
/// </summary>
public interface IParameterCallback
{
    /// <summary>
    /// Allows modification of a parameters collection before the query runs.
    /// </summary>
    void Modify(DbParameterCollection parameters);
}

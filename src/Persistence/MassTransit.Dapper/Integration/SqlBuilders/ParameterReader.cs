namespace MassTransit.Dapper.Integration.SqlBuilders;

using System.Data.Common;


public class ParameterReader
{
    public static IEnumerable<KeyValuePair<string, object?>> Read(object? input)
    {
        return input switch
        {
            null => [],
            IEnumerable<KeyValuePair<string, object?>> sequence => sequence,    // handles Dictionary<string, object> and similar
            DbParameterCollection collection => IterateCollection(collection),  // allows passing in our own parameters collection
            _ => IterateProperties(input)                                       // Dapper-style parameters object
        };

        static IEnumerable<KeyValuePair<string, object?>> IterateCollection(DbParameterCollection col)
        {
            foreach (DbParameter p in col)
                yield return new KeyValuePair<string, object?>(p.ParameterName, p.Value);
        }

        static IEnumerable<KeyValuePair<string, object?>> IterateProperties(object obj)
        {
            return obj
                .GetType()
                .GetProperties()
                .Where(p => p.CanRead)
                .Select(p => new KeyValuePair<string, object?>(
                    p.Name,
                    p.GetValue(obj)
                ));
        }
    }
}

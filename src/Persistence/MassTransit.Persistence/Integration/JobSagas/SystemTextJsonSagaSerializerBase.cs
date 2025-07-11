namespace MassTransit.Dapper.Integration.JobSagas;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


/// <summary>
/// Implements core functionality for a saga serializer, using System.Text.Json.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TModel"></typeparam>
public abstract class SystemTextJsonSagaSerializerBase<TSaga, TModel> : SagaSerializer<TSaga, TModel>
    where TSaga : class
    where TModel : class, ISaga
{

    // All job saga types will serialize similarly -- these options are
    // at the field level, not the entire object level.
    // ReSharper disable once StaticMemberInGenericType
    protected static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        IncludeFields = false,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    };

    protected Uri? UriOrDefault(string? uri) => string.IsNullOrWhiteSpace(uri) 
        ? null 
        : new Uri(uri);

    protected virtual T? Deserialize<T>(string? value)
    {
        if (value is null) return default;

        return JsonSerializer.Deserialize<T>(
            Encoding.UTF8.GetBytes(value), SerializerOptions
        );
    }

    protected virtual string? Serialize<T>(T? value)
    {
        if (value is null) return null;

        return Encoding.UTF8.GetString(
            JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions)
        );
    }

    public abstract TModel FromSaga(TSaga instance);
    public abstract TSaga? FromModel(TModel? model);
}

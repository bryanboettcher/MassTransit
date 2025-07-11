namespace MassTransit.Persistence.Integration.JobSagas
{
    /// <summary>
    /// Used to adapt certain saga types to a format the underlying data store is more suited for.
    /// Usually used when a saga is serializing a small collection that doesn't join to anything
    /// in the database, so this can (de)serialize to a JSON object.
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    /// <typeparam name="TModel"></typeparam>
    public interface SagaSerializer<TSaga, TModel>
        where TSaga : class
        where TModel : class, ISaga
    {
        /// <summary>
        /// Produce the storage-friendly model from the code-friendly saga.
        /// </summary>
        TModel FromSaga(TSaga instance);

        /// <summary>
        /// Produce the code-friendly saga from the storage-friendly model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        TSaga? FromModel(TModel? model);
    }
}

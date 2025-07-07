namespace MassTransit.Dapper.Configuration
{
    /// <summary>
    /// Enables an ADO.NET-based saga repository for neurotic-levels of control over the process.
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    public interface IDapperRepositoryConfigurator<TSaga>
        where TSaga : class
    {
        /// <summary>
        /// Allows for full control over creating the saga repository.  Only use this if the
        /// other configuration methods are insufficiently flexible to configure the repository.
        /// </summary>
        IDapperRepositoryConfigurator<TSaga> SetContextFactory(DatabaseContextFactory<TSaga> contextFactory);
    }
}

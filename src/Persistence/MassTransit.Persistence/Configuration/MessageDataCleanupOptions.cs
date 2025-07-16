namespace MassTransit.Persistence.Configuration
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Controls how the optional MessageData cleanup service behaves.
    /// </summary>
    public class MessageDataCleanupOptions
    {
        public const string Section = nameof(MessageDataCleanupOptions);

        public MessageDataCleanupOptions()
        {
            Enabled = true;
            BatchSize = null;
            Timeout = TimeSpan.FromSeconds(30);
            Interval = TimeSpan.FromMinutes(60);
            StartDelay = TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// If `false`, then the cleanup service will not do anything even
        /// if it's started up.  Defaults to `true`.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// How many entries to delete at once.  Set to `null` for no limit.
        /// </summary>
        public uint? BatchSize { get; set; }

        /// <summary>
        /// Cancels the operation if it runs longer than this value.  Defaults to 30 seconds.
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// How often the cleanup operation is started.  Defaults to every 60 minutes.
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// How long the cleanup service waits to start running after the bus is started.  Helps
        /// prevent trying to clean up data that hasn't even had a chance to be added yet.
        /// Defaults to 5 minutes.
        /// </summary>
        public TimeSpan StartDelay { get; set; }
    }

    /// <summary>
    /// Handles default configuration binding during startup.
    /// </summary>
    public class MessageDataCleanupOptionsConfigurator : IConfigureOptions<MessageDataCleanupOptions>
    {
        readonly IConfiguration _config;

        public MessageDataCleanupOptionsConfigurator(IConfiguration config)
            => _config = config;

        /// <inheritdoc />
        public void Configure(MessageDataCleanupOptions options)
        {
            var section = _config.GetSection(MessageDataCleanupOptions.Section);

            if (section.Exists())
                section.Bind(options);
        }
    }

    /// <summary>
    /// Handles configuration validation post-registration.
    /// </summary>
    public class MessageDataCleanupOptionsValidator : IValidateOptions<MessageDataCleanupOptions>
    {
        public ValidateOptionsResult Validate(string? name, MessageDataCleanupOptions options)
        {
            return ValidateOptionsResult.Success;
        }
    }
}

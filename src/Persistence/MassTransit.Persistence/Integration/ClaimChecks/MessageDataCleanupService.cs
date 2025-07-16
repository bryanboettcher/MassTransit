namespace MassTransit.Persistence.Integration.ClaimChecks
{
    using Configuration;
    using Logging;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public class MessageDataCleanupService : BackgroundService
    {
        readonly IOptionsMonitor<MessageDataCleanupOptions> _options;
        readonly ICollection<IMessageDataCleaner> _cleaners;
        readonly TimeProvider _timeProvider;

        readonly ILogContext _logger;

        public MessageDataCleanupService(
            IOptionsMonitor<MessageDataCleanupOptions> options,
            IBusControl busControl,
            TimeProvider timeProvider)
        {
            _options = options;
            //_cleaners = cleaners;
            
            _timeProvider = timeProvider;
            _logger = LogContext.CreateLogContext("messagedata-cleanup");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.CurrentValue.Enabled)
                return;

            stoppingToken.ThrowIfCancellationRequested();

            var timeout = _options.CurrentValue.Timeout;
            using var signal = new AutoResetEvent(false);

            await using var timer = _timeProvider.CreateTimer(
                state => ((AutoResetEvent)state!).Set(),
                signal,
                _options.CurrentValue.StartDelay,
                _options.CurrentValue.Interval
            );

            while (!stoppingToken.IsCancellationRequested)
            {
                using var timeoutTokenSource = new CancellationTokenSource(
                    timeout,
                    _timeProvider
                );
                
                using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    stoppingToken,
                    timeoutTokenSource.Token
                );
                
                signal.WaitOne();
                signal.Reset();
                
                await RunCleanup(tokenSource.Token)
                    .ConfigureAwait(false);
            }
        }

        async Task RunCleanup(CancellationToken cancellationToken)
        {
            _logger.Info?.Log("Beginning MessageData cleaning operation");

            foreach (var cleaner in _cleaners)
            {
                var name = cleaner.GetType().Name;
                var start = _timeProvider.GetTimestamp();

                var count = 0;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    count = await cleaner.CleanupAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    _logger.Error?.Log(e, "Cleanup {name} failed with an exception: {message}", e.Message);
                }

                var duration = _timeProvider.GetElapsedTime(start);

                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Error?.Log("Cleanup {name} was cancelled after {duration}", name, duration);
                }
                else
                {
                    _logger.Info?.Log("Cleanup {name} pruned {count} items in {duration}", name, count, duration);
                }
            }
        }

        // for a configurator test to verify behavior
        internal IReadOnlyCollection<IMessageDataCleaner> Cleaners
            => (IReadOnlyCollection<IMessageDataCleaner>) _cleaners;
    }
}

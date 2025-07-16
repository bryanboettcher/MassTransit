namespace MassTransit.Persistence.Tests.IntegrationTests.ClaimChecks
{
    using Configuration;
    using Integration.ClaimChecks;
    using Microsoft.Extensions.Options;
    using NUnit.Framework;

    [TestFixture(Explicit = true)]
    [Category("Integration")]
    [Category("Flaky")]
    public class CleanupServiceTests
    {
        protected MessageDataCleanupService Subject = null!;

        protected MessageDataCleanupOptions Options;
        protected IOptionsMonitor<MessageDataCleanupOptions> OptionsMonitor;
        protected FakeTrackingCleaner Cleaner;
        protected TimeProvider TimeProvider;

        public CleanupServiceTests()
        {
            Options = new MessageDataCleanupOptions
            {
                StartDelay = TimeSpan.FromSeconds(0),
                Interval = TimeSpan.FromSeconds(0),
                Timeout = TimeSpan.FromSeconds(0),
            };

            OptionsMonitor = new FakeOptionsMonitor<MessageDataCleanupOptions>(Options);
            TimeProvider = TimeProvider.System;
            Cleaner = new FakeTrackingCleaner(TimeProvider);

            //Subject = new MessageDataCleanupService(OptionsMonitor, [Cleaner], TimeProvider);
        }
        
        [Test]
        public async Task Enabled_behaves_correctly()
        {
            Cleaner.ExecutionCount = 0;
            Options.Enabled = false;

            var deadTokenSource = new CancellationTokenSource();
            deadTokenSource.Cancel();

            Assert.DoesNotThrowAsync(async () =>
            {
                await Subject.StartAsync(deadTokenSource.Token);
            });

            Assert.That(Cleaner.WasExecuted, Is.False);
        }

        [Test]
        public async Task Cleaner_will_happy_path()
        {
            Cleaner.ExecutionCount = 0;
            Cleaner.ExpectedDelay = TimeSpan.FromSeconds(1);

            Options.Enabled = true;
            Options.StartDelay = TimeSpan.Zero;         // immediate startup
            Options.Interval = TimeSpan.FromSeconds(1); // aggressive restart
            Options.Timeout = TimeSpan.FromSeconds(2); 

            var hostTokenSource = new CancellationTokenSource();
            hostTokenSource.CancelAfter(TimeSpan.FromSeconds(1));

            await Subject.StartAsync(hostTokenSource.Token);
            
            if (Subject.ExecuteTask is not null)
                await Subject.ExecuteTask;

            Assert.That(Cleaner.WasExecuted, Is.True);
            Assert.That(Cleaner.ExecutionCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Cleaner_will_restart_timeouts()
        {
            Cleaner.ExecutionCount = 0;
            Cleaner.ExpectedDelay = TimeSpan.FromSeconds(1.5);

            Options.Interval = TimeSpan.FromSeconds(1);
            Options.Timeout = TimeSpan.FromSeconds(1);

            var hostTokenSource = new CancellationTokenSource();
            hostTokenSource.CancelAfter(TimeSpan.FromSeconds(5));

            await Subject.StartAsync(hostTokenSource.Token);

            if (Subject.ExecuteTask is not null)
                await Subject.ExecuteTask;

            Assert.That(Cleaner.WasExecuted, Is.True);
            Assert.That(Cleaner.ExecutionCount, Is.AtLeast(2));
        }
    }

    public class FakeTrackingCleaner : IMessageDataCleaner
    {
        readonly TimeProvider _timeProvider;

        public FakeTrackingCleaner(TimeProvider timeProvider)
            => _timeProvider = timeProvider;

        public int ExpectedRows { get; set; }
        public TimeSpan ExpectedDelay { get; set; }
        public bool WasExecuted => ExecutionCount > 0;
        public int ExecutionCount { get; set; }
        
        public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(ExpectedDelay, _timeProvider);
            ExecutionCount++;
            return ExpectedRows;
        }
    }

    public class FakeOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        public FakeOptionsMonitor(TOptions currentValue)
            => CurrentValue = currentValue;

        public TOptions Get(string? name)
            => CurrentValue;

        public IDisposable? OnChange(Action<TOptions, string?> listener)
            => throw new NotImplementedException();

        public TOptions CurrentValue { get; }
    }
}

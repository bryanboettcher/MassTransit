namespace MassTransit.Persistence.Tests.ComponentTests.CleanupService
{
    using Configuration;
    using Integration.ClaimChecks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using NUnit.Framework;
    using Persistence.SqlServer.Components.ClaimChecks;
    using Persistence.SqlServer.Configuration;
    
    public class CleanupServiceConfigurator_Tests 
    {
        [Test]
        public async Task Registration_behaves_correctly()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
            services.AddSingleton(TimeProvider.System);

            services.AddMassTransit(bus =>
            {
                bus.AddCleanupService();

                bus.UsingInMemory((ctx, cfg) =>
                {
                    cfg.UseMessageData(selector => selector.UsingSqlServer("my connection string"));

                    cfg.ConfigureEndpoints(ctx);
                });
            });

            var provider = services.BuildServiceProvider();
            var subject = provider.GetRequiredService<IHostedService>()
                as MessageDataCleanupService;

            Assert.That(subject, Is.Not.Null);
            Assert.That(subject!.Cleaners, Has.One.Items);
            Assert.That(subject!.Cleaners.First(), Is.TypeOf<SqlServerMessageDataRepository>());
        }
    }
}

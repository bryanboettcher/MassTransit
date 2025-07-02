namespace MassTransit.DapperIntegration.Tests.ComponentTests
{
    using System.Threading.Tasks;
    using Dapper.Configuration;
    using Dapper.SqlServer;
    using IntegrationTests.StateMachines;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;


    [TestFixture]
    public class Configurator_Tests
    {
        [Test]
        public async Task Options_can_be_configured()
        {
            var services = new ServiceCollection();

            services.AddMassTransit(bus =>
            {
                bus.AddSagaStateMachine<VersionedSagaStateMachine, VersionedBehaviorSaga>()
                    .DapperRepository(conf => conf);
            });

            var provider = services.BuildServiceProvider();

        }
    }
}

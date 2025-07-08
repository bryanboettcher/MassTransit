namespace MassTransit.Dapper.Tests.ComponentTests
{
    using System.Threading.Tasks;
    using IntegrationTests.StateMachineSagas;
    using MassTransit.Dapper.Configuration;
    using MassTransit.Dapper.SqlServer.Configuration;
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
                    .DapperRepository(conf => conf.UsingSqlServer(
                        sql => sql.SetConnectionString("Server=localhost; User Id=sa;")
                            .SetTableName("VersionedSaga")
                    )
                );
            });

            var provider = services.BuildServiceProvider();

        }
    }
}

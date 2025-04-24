namespace MassTransit.DapperIntegration.Tests.ComponentTests
{
    using System.Data;
    using IntegrationTests;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;


    [TestFixture]
    public class Configurator_Tests
    {
        [Test]
        public void Extension_method_properly_registers()
        {
            var services = new ServiceCollection();

            services.AddMassTransit(bus =>
            {
                //bus.AddSagaStateMachine<VersionedSagaStateMachine, VersionedBehaviorSaga>()
                //    .DapperRepository(cfg => cfg
                //        .UseSqlServer("connection string")
                //        .UseIsolationLevel(IsolationLevel.ReadCommitted)
                //        .UseTableName("dbo.VersionedSagas")
                //    );

                bus.AddSagaStateMachine<VersionedSagaStateMachine, VersionedBehaviorSaga>()
                    .DapperRepository("string");
            });
        }
    }
}

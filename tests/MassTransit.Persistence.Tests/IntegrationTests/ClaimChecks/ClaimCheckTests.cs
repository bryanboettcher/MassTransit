using MassTransit.Persistence.Tests.IntegrationTests.Connectors;
using NUnit.Framework;

namespace MassTransit.Persistence.Tests.IntegrationTests.ClaimChecks
{
    using System.Security.Cryptography;
    using Microsoft.Extensions.Time.Testing;
    
    [TestFixture(typeof(PessimisticSqlServerConnector))]
    [TestFixture(typeof(PessimisticPostgresConnector))]
    [TestFixture(typeof(PessimisticMySqlConnector))]
    public class ClaimCheck_Tests<TConnector> : SagaTests<TConnector>
        where TConnector : TestConnector, new()
    {
        readonly FakeTimeProvider _timeProvider;
        readonly IMessageDataRepository _repository;
        
        public ClaimCheck_Tests()
        {
            _timeProvider = new FakeTimeProvider();
            _timeProvider.SetUtcNow(DateTimeOffset.UtcNow);

            _repository = Connector.CreateMessageDataRepository(_timeProvider);
        }

        [Test]
        public async Task Data_stored_is_available_and_unmodified()
        {
            var buffer = new byte[128 * 1024];
            Random.Shared.NextBytes(buffer);

            var writeHash = SHA1.HashData(buffer);

            await using var writeStream = new MemoryStream();
            await writeStream.WriteAsync(buffer, CancellationToken.None);

            writeStream.Position = 0;
            var uri = await _repository.Put(writeStream);

            Assert.That(uri.ToString(), Is.Not.Null);
            Assert.That(uri.Scheme, Is.EqualTo("urn"));

            await using var readStream = await _repository.Get(uri);
            var readHash = await SHA1.HashDataAsync(readStream);

            Assert.That(writeHash, Is.EqualTo(readHash));
        }

        [Test]
        public async Task Data_stored_will_expire()
        {
            var buffer = new byte[128 * 1024];
            Random.Shared.NextBytes(buffer);

            await using var writeStream = new MemoryStream();
            await writeStream.WriteAsync(buffer, CancellationToken.None);

            writeStream.Position = 0;
            var uri = await _repository.Put(writeStream, TimeSpan.FromSeconds(10));

            Assert.That(uri.ToString(), Is.Not.Null);
            Assert.That(uri.Scheme, Is.EqualTo("urn"));

            _timeProvider.Advance(TimeSpan.FromSeconds(30));

            Assert.ThrowsAsync<KeyNotFoundException>(
                async () => _ = await _repository.Get(uri)
            );
        }

        [Test]
        public async Task Data_already_expired_is_not_stored()
        {
            var buffer = new byte[128 * 1024];
            Random.Shared.NextBytes(buffer);

            await using var writeStream = new MemoryStream();
            await writeStream.WriteAsync(buffer, CancellationToken.None);

            writeStream.Position = 0;
            var expected = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _repository.Put(writeStream, TimeSpan.FromSeconds(-10))
            );

            Assert.That(expected, Is.Not.Null);
            Assert.That(expected?.Message, Is.EqualTo("TTL has already expired"));
        }
    }
}

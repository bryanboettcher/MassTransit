namespace MassTransit.DapperIntegration.Tests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Dapper;
    using MassTransit.Tests;
    using Microsoft.Data.SqlClient;
    using NUnit.Framework;
    using TestFramework;


    public abstract class DapperVersionedSagaTests : InMemoryTestFixture
    {
        protected readonly string ConnectionString;
        protected readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);
        
        protected static readonly Guid SagaId = Guid.Parse("d747db39-0d64-49b5-85f4-2a796ba82130");

        protected DapperVersionedSagaTests()
        {
            ConnectionString = LocalDbConnectionStringProvider.GetLocalDbConnectionString();
        }
        
        [OneTimeSetUp]
        public async Task Initialize()
        {
            await using var connection = new SqlConnection(ConnectionString);
            var sql = @"DROP TABLE IF EXISTS VersionedSagas;

CREATE TABLE VersionedSagas (
    [CorrelationId] UNIQUEIDENTIFIER NOT NULL,
    [Version] INT NOT NULL,
    [CurrentState] VARCHAR(20),

    [Name] NVARCHAR(MAX),
    
    PRIMARY KEY CLUSTERED (CorrelationId)
);";

            await connection.ExecuteAsync(sql);
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            await using var connection = new SqlConnection(ConnectionString);
            var sql = @"DROP TABLE IF EXISTS VersionedSagas;";
            await connection.ExecuteAsync(sql);
        }

        [SetUp]
        public async Task Setup()
        {
            await using var connection = new SqlConnection(ConnectionString);
            var sql = @"TRUNCATE TABLE VersionedSagas;";
            await connection.ExecuteAsync(sql);
        }

        protected async Task<List<TSaga>> GetSagas<TSaga>() where TSaga : class, ISaga
        {
            await using var connection = new SqlConnection(ConnectionString);
            var sql = "SELECT * FROM VersionedSagas;";
            return (await connection.QueryAsync<TSaga>(sql)).AsList();
        }
    }
}

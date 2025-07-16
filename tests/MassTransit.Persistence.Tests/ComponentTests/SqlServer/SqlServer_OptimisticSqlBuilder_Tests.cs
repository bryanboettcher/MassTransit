namespace MassTransit.Persistence.Tests.ComponentTests.SqlServer
{
    using Integration.Saga;
    using NUnit.Framework;
    using Persistence.SqlServer.Connections;


    [TestFixture]
    public class SqlServer_OptimisticSqlBuilder_Tests : SqlServer_Tests
    {
        public class VersionedSaga_SqlBuilder
        {
            protected SagaDatabaseContext<VersionedSaga> Subject =
                new OptimisticSqlServerDatabaseContext<VersionedSaga>("", "VersionedSagas", "CorrelationId", "RowVersion", "RowVersion");

            [Test]
            public void Insert_builds_correct_sql()
            {
                var actual = Subject.BuildInsertSql();
                var expected =
                    "INSERT INTO VersionedSagas ([CorrelationId], [Name], [Age], [PhoneNumber], [Zip_Code]) VALUES (@correlationid, @name, @age, @phonenumber, @zipcode)";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Update_builds_correct_sql()
            {
                var actual = Subject.BuildUpdateSql();
                var expected =
                    "UPDATE VersionedSagas SET [Name] = @name, [Age] = @age, [PhoneNumber] = @phonenumber, [Zip_Code] = @zipcode WHERE [CorrelationId] = @correlationid AND [RowVersion] = @rowversion";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Delete_builds_correct_sql()
            {
                var actual = Subject.BuildDeleteSql();
                var expected = "DELETE FROM VersionedSagas WHERE [CorrelationId] = @correlationid AND [RowVersion] = @rowversion";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Load_builds_correct_sql()
            {
                var actual = Subject.BuildLoadSql();
                var expected = "SELECT TOP 1 * FROM VersionedSagas WHERE [CorrelationId] = @correlationid";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Query_builds_correct_sql()
            {
                var actual = Subject.BuildQuerySql(x => x.Name == "test", null);
                var expected = "SELECT * FROM VersionedSagas WHERE [Name] = @value0";

                Assert.That(actual, Is.EqualTo(expected));
            }
        }
    }
}

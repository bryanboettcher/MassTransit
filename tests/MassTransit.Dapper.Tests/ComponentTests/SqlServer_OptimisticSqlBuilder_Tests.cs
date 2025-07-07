using MassTransit.DapperIntegration.SqlBuilders;
using MassTransit.DapperIntegration.Tests.Common;
using NUnit.Framework;

namespace MassTransit.Dapper.Tests.ComponentTests
{
    using SqlServer.Formatting;


    [TestFixture]
    public class SqlServer_OptimisticSqlBuilder_Tests
    {
        public class VersionedSaga_SqlBuilder
        {
            protected ISagaSqlFormatter<VersionedSaga> Subject = new OptimisticSqlServerSagaFormatter<VersionedSaga>();

            [Test]
            public void Insert_builds_correct_sql()
            {
                var actual = Subject.BuildInsertSql();
                var expected = "INSERT INTO VersionedSagas ([CorrelationId], [Name], [Age], [PhoneNumber], [Zip_Code]) VALUES (@correlationId, @name, @age, @phoneNumber, @zipCode)";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Update_builds_correct_sql()
            {
                var actual = Subject.BuildUpdateSql();
                var expected = "UPDATE VersionedSagas SET [Name] = @name, [Age] = @age, [PhoneNumber] = @phoneNumber, [Zip_Code] = @zipCode WHERE [CorrelationId] = @correlationId AND [RowVersion] = @rowversion";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Delete_builds_correct_sql()
            {
                var actual = Subject.BuildDeleteSql();
                var expected = "DELETE FROM VersionedSagas WHERE [CorrelationId] = @correlationId AND [RowVersion] = @rowversion";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Load_builds_correct_sql()
            {
                var actual = Subject.BuildLoadSql();
                var expected = "SELECT * FROM VersionedSagas WHERE [CorrelationId] = @correlationId AND [RowVersion] = @rowversion";

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

        public class UnversionedSaga_SqlBuilder
        {
            protected ISagaSqlFormatter<UnversionedSaga> Subject;

            [Test]
            public void Unversioned_properly_errors()
            {
                try
                {
                    Subject = new OptimisticSqlServerSagaFormatter<UnversionedSaga>();
                }
                catch (InvalidOperationException e)
                {
                    Assert.That(e.Message, Contains.Substring("ROWVERSION column was not auto-detected").IgnoreCase);
                    return;
                }

                Assert.Fail("Exception should have been thrown");
            }
        }

    }
}

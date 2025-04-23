namespace MassTransit.DapperIntegration.Tests.ComponentTests
{
    using System;
    using SqlBuilders;
    using NUnit.Framework;
    using Saga;


    [TestFixture]
    public class SqlServer_SqlBuilder_Tests
    {
        public class VersionedSaga_SqlBuilder
        {
            protected SqlBuilder<VersionedSaga> Subject = new SqlServerBuilder<VersionedSaga>();
            
            [Test]
            public void Insert_builds_correct_sql()
            {
                var actual = Subject.BuildInsertSql();
                var expected = "INSERT INTO VersionedSagas ([CorrelationId], [Version], [Name], [Age], [PhoneNumber], [Zip_Code]) VALUES (@correlationId, @version, @name, @age, @phoneNumber, @zipCode)";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Update_builds_correct_sql()
            {
                var actual = Subject.BuildUpdateSql();
                var expected = "UPDATE VersionedSagas SET [Name] = @name, [Age] = @age, [PhoneNumber] = @phoneNumber, [Zip_Code] = @zipCode, [Version] = @version WHERE [CorrelationId] = @correlationId AND [Version] < @version";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Delete_builds_correct_sql()
            {
                var actual = Subject.BuildDeleteSql();
                var expected = "DELETE FROM VersionedSagas WHERE [CorrelationId] = @correlationId AND [Version] < @version";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Load_builds_correct_sql()
            {
                var actual = Subject.BuildLoadSql();
                var expected = "SELECT * FROM VersionedSagas WITH (UPDLOCK, ROWLOCK) WHERE [CorrelationId] = @correlationId";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Query_builds_correct_sql()
            {
                var actual = Subject.BuildQuerySql(x => x.Name == "test", null);
                var expected = "SELECT * FROM VersionedSagas WITH (UPDLOCK, ROWLOCK) WHERE [Name] = @value0";

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        public class UnversionedSaga_SqlBuilder
        {
            protected SqlBuilder<UnversionedSaga> Subject = new SqlServerBuilder<UnversionedSaga>();

            [Test]
            public void Insert_builds_correct_sql()
            {
                var actual = Subject.BuildInsertSql();
                var expected = "INSERT INTO UnversionedSagas ([CorrelationId], [Name], [EarthTrips], [PhoneNumber], [Zip_Code]) VALUES (@correlationId, @name, @age, @phoneNumber, @zipCode)";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Update_builds_correct_sql()
            {
                var actual = Subject.BuildUpdateSql();
                var expected = "UPDATE UnversionedSagas SET [Name] = @name, [EarthTrips] = @age, [PhoneNumber] = @phoneNumber, [Zip_Code] = @zipCode WHERE [CorrelationId] = @correlationId";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Delete_builds_correct_sql()
            {
                var actual = Subject.BuildDeleteSql();
                var expected = "DELETE FROM UnversionedSagas WHERE [CorrelationId] = @correlationId";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Load_builds_correct_sql()
            {
                var actual = Subject.BuildLoadSql();
                var expected = "SELECT * FROM UnversionedSagas WITH (UPDLOCK, ROWLOCK) WHERE [CorrelationId] = @correlationId";

                Assert.That(actual, Is.EqualTo(expected));
            }

            [Test]
            public void Query_builds_correct_sql()
            {
                var actual = Subject.BuildQuerySql(x => x.Name == "test" && x.Age < 99, null);
                var expected = "SELECT * FROM UnversionedSagas WITH (UPDLOCK, ROWLOCK) WHERE [Name] = @value0 AND [EarthTrips] < @value1";

                Assert.That(actual, Is.EqualTo(expected));
            }
        }
        
        public class Complex_SqlBuilder
        {
            protected SqlBuilder<ComplexSaga> Subject = new SqlServerBuilder<ComplexSaga>();


            [Test]
            public void ComplexExpressions_behave_properly()
            {
                var m = new { Start = new DateTime(2025, 04, 22), End = new DateTime(2025, 05, 22) };
                var actual = Subject.BuildQuerySql(x => x.Name == "test" && x.Age <= 99 && x.StartDate > m.Start && x.EndDate < m.End, null);
                var expected = "SELECT * FROM OverrideTable WITH (UPDLOCK, ROWLOCK) WHERE [Name] = @value0 AND [Age] <= @value1 AND [StartDate] > @value2 AND [EndDate] < @value3";

                Assert.That(actual, Is.EqualTo(expected));
            }
        }
    }
}

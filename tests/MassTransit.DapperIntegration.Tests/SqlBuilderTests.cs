namespace MassTransit.DapperIntegration.Tests
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using Dapper.Contrib.Extensions;
    using NUnit.Framework;
    using Saga;

    [TestFixture]
    public class SqlBuilderTests
    {
        public class VersionedSaga_SqlBuilder
        {
            public class VersionedSaga : ISagaVersion
            {
                public Guid CorrelationId { get; set; }
                public int Version { get; set; }
                public string Name { get; set; }
                public int Age { get; set; }
                public string PhoneNumber { get; set; }
                public string Zip_Code { get; set; }
            }

            protected SqlBuilder<VersionedSaga> Subject = new SagaDatabaseContext<VersionedSaga>(null, null);
            
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
                var actual = Subject.BuildQuerySql(x => x.Name == "test", out _);
                var expected = "SELECT * FROM VersionedSagas WITH (UPDLOCK, ROWLOCK) WHERE [Name] = @value0";

                Assert.That(actual, Is.EqualTo(expected));
            }
        }

        public class UnversionedSaga_SqlBuilder
        {
            public class UnversionedSaga : ISaga
            {
                public Guid CorrelationId { get; set; }
                public string Name { get; set; }
                [Column("EarthTrips")]
                public int Age { get; set; }
                public string PhoneNumber { get; set; }
                public string Zip_Code { get; set; }
            }

            protected SqlBuilder<UnversionedSaga> Subject = new SagaDatabaseContext<UnversionedSaga>(null, null);

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
                var actual = Subject.BuildQuerySql(x => x.Name == "test" && x.Age < 99, out _);
                var expected = "SELECT * FROM UnversionedSagas WITH (UPDLOCK, ROWLOCK) WHERE [Name] = @value0 AND [EarthTrips] < @value1";

                Assert.That(actual, Is.EqualTo(expected));
            }
        }
        
        public class Complex_SqlBuilder
        {
            [Dapper.Contrib.Extensions.Table("OverrideTable")]
            public class ComplexSaga : ISaga
            {
                public Guid CorrelationId { get; set; }
                public string Name { get; set; }
                public int Age { get; set; }
                public DateTime StartDate { get; set; }
                public DateTime EndDate { get; set; }
                public bool IsActive { get; set; }
            }

            protected SqlBuilder<ComplexSaga> Subject = new SagaDatabaseContext<ComplexSaga>(null, null);


            [Test]
            public void ComplexExpressions_behave_properly()
            {
                var m = new { Start = new DateTime(2025, 04, 22), End = new DateTime(2025, 05, 22) };
                var actual = Subject.BuildQuerySql(x => (x.Name == "test" && x.Age <= 99) && (x.StartDate > m.Start && x.EndDate < m.End), out _);
                var expected = "SELECT * FROM OverrideTable WITH (UPDLOCK, ROWLOCK) WHERE [Name] = @value0 AND [Age] <= @value1 AND [StartDate] > @value2 AND [EndDate] < @value3";

                Assert.That(actual, Is.EqualTo(expected));
            }
        }
    }
}

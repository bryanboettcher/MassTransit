namespace MassTransit.DapperIntegration.Tests.ComponentTests;

using System;
using NUnit.Framework;
using SqlBuilders;


[TestFixture]
public class Postgres_SqlBuilder_Tests
{
    public class VersionedSaga_SqlBuilder
    {
        protected SqlBuilder<VersionedSaga> Subject = new PostgresBuilder<VersionedSaga>();

        [Test]
        public void Insert_builds_correct_sql()
        {
            var actual = Subject.BuildInsertSql();
            var expected = "INSERT INTO VersionedSagas (CorrelationId, Version, Name, Age, PhoneNumber, Zip_Code) VALUES ($1, $2, $3, $4, $5, $6)";

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Update_builds_correct_sql()
        {
            var actual = Subject.BuildUpdateSql();
            var expected = "UPDATE VersionedSagas SET Version = $2, Name = $3, Age = $4, PhoneNumber = $5, Zip_Code = $6 WHERE CorrelationId = $1 AND Version < $2";

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Delete_builds_correct_sql()
        {
            var actual = Subject.BuildDeleteSql();
            var expected = "DELETE FROM VersionedSagas WHERE CorrelationId = $1 AND Version < $2";

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Load_builds_correct_sql()
        {
            var actual = Subject.BuildLoadSql();
            var expected = "SELECT * FROM VersionedSagas WHERE CorrelationId = $1 FOR UPDATE";

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Query_builds_correct_sql()
        {
            var actual = Subject.BuildQuerySql(x => x.Name == "test", null);
            var expected = "SELECT * FROM VersionedSagas WHERE Name = $1 FOR UPDATE";

            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    public class UnversionedSaga_SqlBuilder
    {
        protected SqlBuilder<UnversionedSaga> Subject = new PostgresBuilder<UnversionedSaga>();

        [Test]
        public void Insert_builds_correct_sql()
        {
            var actual = Subject.BuildInsertSql();
            var expected = "INSERT INTO UnversionedSagas (CorrelationId, Name, EarthTrips, PhoneNumber, Zip_Code) VALUES ($1, $2, $3, $4, $5)";

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Update_builds_correct_sql()
        {
            var actual = Subject.BuildUpdateSql();
            var expected = "UPDATE UnversionedSagas SET Name = $2, EarthTrips = $3, PhoneNumber = $4, Zip_Code = $5 WHERE CorrelationId = $1";

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Delete_builds_correct_sql()
        {
            var actual = Subject.BuildDeleteSql();
            var expected = "DELETE FROM UnversionedSagas WHERE CorrelationId = $1";

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Load_builds_correct_sql()
        {
            var actual = Subject.BuildLoadSql();
            var expected = "SELECT * FROM UnversionedSagas WHERE CorrelationId = $1 FOR UPDATE";

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Query_builds_correct_sql()
        {
            var actual = Subject.BuildQuerySql(x => x.Name == "test" && x.Age < 99, null);
            var expected = "SELECT * FROM UnversionedSagas WHERE Name = $1 AND EarthTrips < $2 FOR UPDATE";

            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    public class Complex_SqlBuilder
    {
        protected SqlBuilder<ComplexSaga> Subject = new PostgresBuilder<ComplexSaga>();


        [Test]
        public void ComplexExpressions_behave_properly()
        {
            var m = new { Start = new DateTime(2025, 04, 22), End = new DateTime(2025, 05, 22) };
            var actual = Subject.BuildQuerySql(x => x.Name == "test" && x.Age <= 99 && x.StartDate > m.Start && x.EndDate < m.End, null);
            var expected = "SELECT * FROM OverrideTable WHERE Name = $1 AND Age <= $2 AND StartDate > $3 AND EndDate < $4 FOR UPDATE";

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}

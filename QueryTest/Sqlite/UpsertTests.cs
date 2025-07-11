﻿using BaseTest.Fixtures;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Sqlite
{
    public class UpsertTests(ITestOutputHelper output, MockFixture<SqliteQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqliteQueryConfig>>
    {
        [Fact]
        public void MergeTest()
        {
            var expected = new SqlExpression("INSERT INTO \"TargetTable\" (\"Name\", \"Description\", \"Status\") SELECT \"Source\".\"Name\", \"Source\".\"Description\", \"Source\".\"Status\" FROM \"SrcTable\" \"Source\" WHERE true ON CONFLICT(\"Id\", \"Description\") SET \"Name\"=\"Source\".\"Name\", \"Description\"=\"Source\".\"Description\"");

            using var query = new Query("TargetTable");

            var result = query.GetGrammar().Upsert(
                new DbName("SrcTable"),
                ["Id", "Description"],
                ["Name", "Description"],
                ["Name", "Description", "Status"]
            );

            QueryAssert.Equal(expected, result);
        }

        [Fact]
        public void MergeWithAlias()
        {
            var expected = new SqlExpression("INSERT INTO \"TargetTable\" (\"Name\", \"Description\", \"Status\") SELECT \"Src\".\"Name\", \"Src\".\"Description\", \"Src\".\"Status\" FROM \"SrcTable\" \"Src\" WHERE true ON CONFLICT(\"Id\", \"Description\") SET \"Name\"=\"Src\".\"Name\", \"Description\"=\"Src\".\"Description\"");

            using var query = new Query("TargetTable Tgt");

            var result = query.GetGrammar().Upsert(
                new DbName("SrcTable Src"),
                ["Id", "Description"],
                ["Name", "Description"],
                ["Name", "Description", "Status"]
            );

            QueryAssert.Equal(expected, result);
        }

        [Fact]
        public void MergeWithRow()
        {
            using var query = new Query("Address");

            Assert.Throws<NotSupportedException>(() => query.GetGrammar().Upsert(
                Tables.Address.RandomRows(5),
                [Tables.Address.ID, Tables.Address.NAME],
                [Tables.Address.NAME, Tables.Address.CITY]
            ));
        }
    }
}

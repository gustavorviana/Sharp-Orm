using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.Data.Common;
using System.Linq;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class SqlServerInsertUpdateDeleteBuilderTest : SqlServerTest
    {
        [TestMethod]
        public void Insert()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);

            q.WhereInColumn(123, "TokenAtacado", "TokenVarejo", "TokenIndustria");

            var sqlExpression = g.Insert(new Cell[] { new Cell(ID, 1), new Cell(NAME, "T1"), new Cell("value", null) });

            TestAssert.AreDecoded("INSERT INTO [TestTable] ([id], [name], [value]) VALUES (1, @p1, NULL); SELECT SCOPE_IDENTITY();", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void InsertWithoutId()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);
            q.ReturnsInsetionId = false;

            q.WhereInColumn(123, "TokenAtacado", "TokenVarejo", "TokenIndustria");

            var sqlExpression = g.Insert(new Cell[] { new Cell(ID, 1), new Cell(NAME, "T1"), new Cell("value", null) });
            TestAssert.AreDecoded("INSERT INTO [TestTable] ([id], [name], [value]) VALUES (1, @p1, NULL)", sqlExpression);

            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void InsertWIthRaw()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);

            var sqlExpression = g.Insert(new Cell[] { new Cell(ID, (SqlExpression)"1") });
            TestAssert.AreDecoded("INSERT INTO [TestTable] ([id]) VALUES (1); SELECT SCOPE_IDENTITY();", sqlExpression);
        }

        [TestMethod]
        public void InsertExtendedClass()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);
            var table = new ExtendedTestTable
            {
                Id = 1,
                CreatedAt = DateTime.Now,
                CustomId = Guid.NewGuid(),
                Name = "Name",
                Number = 2.1M,
                ExtendedProp = "Nothing",
                CustomStatus = Status.Success,
                Nick = null
            };

            var sqlExpression = g.Insert(Row.Parse(table, typeof(TestTable), true, false).Cells);
            TestAssert.AreDecoded("INSERT INTO [TestTable] ([Id], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status]) VALUES (1, @p1, NULL, @p2, 2.1, @p3, 1); SELECT SCOPE_IDENTITY();", sqlExpression);

            TestAssert.AreEqualsParameters(sqlExpression, 0, 1, 2);
        }

        [TestMethod]
        public void BulkInsert()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);
            var rows = new Row[] { NewRow(1, "T1"), NewRow(2, "T2"), NewRow(3, "T3"), NewRow(4, "T4"), NewRow(5, "T5") };

            TestAssert.TestExpected(
                g.BulkInsert(rows),
                "INSERT INTO [TestTable] ([id], [name]) VALUES (1, @p1), (2, @p2), (3, @p3), (4, @p4), (5, @p5)",
                 new object[] { "T1", "T2", "T3", "T4", "T5" }
            );
        }

        private static new Row NewRow(int id, string name)
        {
            return new Row(new Cell(ID, id), new Cell(NAME, name));
        }

        [TestMethod]
        public void UpdateNoColumns()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);

            Assert.ThrowsException<InvalidOperationException>(() => g.Update(Array.Empty<Cell>()));
        }

        [TestMethod]
        public void Update()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);

            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));
            var sqlExpression = g.Update(row.Cells);

            TestAssert.AreDecoded("UPDATE [TestTable] SET [name] = @p1, [alias] = @p2, [value] = NULL, [status] = 1", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0, 1);
        }

        [TestMethod]
        public void UpdateCaseValue()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);
            const string CaseMsg = "Without alias";
            const string ElseMsg = "With alias";

            var caseVal = new Case().When("alias", "IS", null, CaseMsg).Else(ElseMsg);
            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", caseVal), new Cell("value", null), new Cell("status", Status.Success));

            TestAssert.TestExpected(
                g.Update(row.Cells),
                "UPDATE [TestTable] SET [name] = @p1, [alias] = CASE WHEN [alias] IS NULL THEN @p2 ELSE @p3 END, [value] = NULL, [status] = 1",
                new object[]
                {
                    row[0].Value,
                    CaseMsg,
                    ElseMsg
                }
            );
        }

        [TestMethod]
        public void UpdateByColumn()
        {
            using var q = NewQuery(TABLE);
            var g = new MysqlGrammar(q);
            q.Where("id", "=", 1);

            var sqlExpression = g.Update(new Cell[] { new Cell("name", new Column("nick")) });
            TestAssert.AreDecoded("UPDATE [TestTable] SET [name] = [nick] WHERE [id] = 1", sqlExpression);
        }

        [TestMethod]
        public void UpdateWhere()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);
            q.Where("id", "=", 1);

            var sqlExpression = g.Update(new Cell[] { new Cell("name", "MyName") });
            TestAssert.AreDecoded("UPDATE [TestTable] SET [name] = @p1 WHERE [id] = 1", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void Delete()
        {
            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);

            TestAssert.AreDecoded("DELETE FROM [TestTable]", g.Delete());
        }

        [TestMethod]
        public void DeleteLimit()
        {
            using var q = NewQuery(TABLE);
            q.Limit = 5;
            var g = new SqlServerGrammar(q);

            TestAssert.AreDecoded("DELETE TOP(5) FROM [TestTable]", g.Delete());
        }

        [TestMethod]
        public void DeleteWhere()
        {
            using var q = NewQuery(TABLE);
            q.Where("id", "=", 1);
            var g = new SqlServerGrammar(q);

            TestAssert.AreDecoded("DELETE FROM [TestTable] WHERE [id] = 1", g.Delete());
        }

        [TestMethod]
        public void DeleteWhereJoin()
        {
            using var q = NewQuery(TABLE, "t1");
            q.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            q.Where("t2.Id", 1);
            var g = new SqlServerGrammar(q);

            TestAssert.AreDecoded("DELETE [t1] FROM [TestTable] [t1] INNER JOIN [Table2] [t2] ON [t2].[Id] = [t1].[T2Id] WHERE [t2].[Id] = 1", g.Delete());
        }

        [TestMethod]
        public void InsertByBasicSelect()
        {
            using var selectQuery = new Query(Connection, "User");
            selectQuery
                .Select(new Column("Id"), (Column)"1")
                .Where("id", 1);

            using var q = NewQuery(TABLE);
            var g = new SqlServerGrammar(q);

            var sqlExpression = g.InsertQuery(selectQuery, new[] { "UserId", "Status" });
            TestAssert.AreDecoded("INSERT INTO [TestTable] ([UserId], [Status]) SELECT [Id], 1 FROM [User] WHERE [id] = 1", sqlExpression);
        }

        [TestMethod]
        public void SelectGroupByColumnName()
        {
            using var query = new Query(Connection, TABLE);
            query.GroupBy("Col1", "Col2");
            var g = new SqlServerGrammar(query);

            TestAssert.AreDecoded("SELECT * FROM [TestTable] GROUP BY [Col1], [Col2]", g.Select());
        }

        [TestMethod]
        public void SelectHavingColumn()
        {
            using var query = new Query(Connection, TABLE);
            query.GroupBy("Col1", "Col2").Having(q => q.Where("Col1", true));
            var g = new SqlServerGrammar(query);

            TestAssert.AreDecoded("SELECT * FROM [TestTable] GROUP BY [Col1], [Col2] HAVING [Col1] = 1", g.Select());
        }
    }
}

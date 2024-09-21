using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Fb;
using System;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest.Firebird
{
    [TestClass]
    public class FbInsertUpdateDeleteBuilderTest : FbTableTest
    {
        [TestMethod]
        public void Insert()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);

            q.WhereInColumn(123, "TokenAtacado", "TokenVarejo", "TokenIndustria");

            var sqlExpression = g.Insert(new Cell[] { new Cell(ID, 1), new Cell(NAME, "T1"), new Cell("value", null) });

            TestAssert.AreDecoded("INSERT INTO TestTable (id, name, value) VALUES (1, @p1, NULL) RETURNING ID;", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void InsertWithoutId()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);
            q.ReturnsInsetionId = false;

            q.WhereInColumn(123, "TokenAtacado", "TokenVarejo", "TokenIndustria");

            var sqlExpression = g.Insert(new Cell[] { new Cell(ID, 1), new Cell(NAME, "T1"), new Cell("value", null) });
            TestAssert.AreDecoded("INSERT INTO TestTable (id, name, value) VALUES (1, @p1, NULL);", sqlExpression);

            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void InsertWIthRaw()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);

            var sqlExpression = g.Insert(new Cell[] { new Cell(ID, (SqlExpression)"1") });
            TestAssert.AreDecoded("INSERT INTO TestTable (id) VALUES (1) RETURNING ID;", sqlExpression);
        }

        [TestMethod]
        public void InsertExtendedClass()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);
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
            TestAssert.AreDecoded("INSERT INTO TestTable (Id, Name, Nick, record_created, Number, custom_id, custom_status) VALUES (1, @p1, NULL, @p2, 2.1, @p3, 1) RETURNING ID;", sqlExpression);

            TestAssert.AreEqualsParameters(sqlExpression, 0, 1, 2);
        }

        [TestMethod]
        public void BulkInsert()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);
            var rows = new Row[] { NewRow(1, "T1"), NewRow(2, "T2"), NewRow(3, "T3"), NewRow(4, "T4"), NewRow(5, "T5") };

            TestAssert.TestExpected(
                g.BulkInsert(rows),
                "EXECUTE BLOCK (p1 CHAR(2) = @p1, p2 CHAR(2) = @p2, p3 CHAR(2) = @p3, p4 CHAR(2) = @p4, p5 CHAR(2) = @p5)AS BEGIN INSERT INTO TestTable (id, name) VALUES (1, :p1);INSERT INTO TestTable (id, name) VALUES (2, :p2);INSERT INTO TestTable (id, name) VALUES (3, :p3);INSERT INTO TestTable (id, name) VALUES (4, :p4);INSERT INTO TestTable (id, name) VALUES (5, :p5);END",
                 new object[] { "T1", "T2", "T3", "T4", "T5" }
            );
        }

        private static Row NewRow(int id, string name)
        {
            return new Row(new Cell(ID, id), new Cell(NAME, name));
        }

        [TestMethod]
        public void UpdateNoColumns()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);

            Assert.ThrowsException<InvalidOperationException>(() => g.Update(Array.Empty<Cell>()));
        }

        [TestMethod]
        public void Update()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);

            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));
            var sqlExpression = g.Update(row.Cells);
            TestAssert.AreDecoded("UPDATE TestTable SET name = @p1, alias = @p2, value = NULL, status = 1", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0, 1);
        }

        [TestMethod]
        public void UpdateWhereJoin()
        {
            using var q = NewQuery(TABLE, "t1");
            q.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            q.Where("t2.Id", 1);
            var g = new FbGrammar(q);

            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", "Test"), new Cell("value", null), new Cell("status", Status.Success));
            Assert.ThrowsException<NotSupportedException>(() => g.Update(row.Cells));
        }

        [TestMethod]
        public void UpdateCaseValue()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);
            const string CaseMsg = "Without alias";
            const string ElseMsg = "With alias";

            var caseVal = new Case().When("alias", "IS", null, CaseMsg).Else(ElseMsg);
            var row = new Row(new Cell("name", "MyTestName"), new Cell("alias", caseVal), new Cell("value", null), new Cell("status", Status.Success));
            var sqlExpression = g.Update(row.Cells);
            TestAssert.AreDecoded("UPDATE TestTable SET name = @p1, alias = CASE WHEN alias IS NULL THEN @p2 ELSE @p3 END, value = NULL, status = 1", sqlExpression);

            TestAssert.AreEqualsParameters(sqlExpression, 0, 1, 2);
        }

        [TestMethod]
        public void UpdateByColumn()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);
            q.Where("id", "=", 1);

            var sqlExpression = g.Update(new Cell[] { new Cell("name", new Column("nick")) });
            TestAssert.AreDecoded("UPDATE TestTable SET name = nick WHERE id = 1", sqlExpression);
        }

        [TestMethod]
        public void UpdateWhere()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);
            q.Where("id", "=", 1);

            var sqlExpression = g.Update(new Cell[] { new Cell("name", "MyName") });
            TestAssert.AreDecoded("UPDATE TestTable SET name = @p1 WHERE id = 1", sqlExpression);

            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void Delete()
        {
            using var q = NewQuery();
            var g = new FbGrammar(q);

            var sqlExpression = g.Delete();
            TestAssert.AreDecoded("DELETE FROM TestTable", sqlExpression);
        }

        [TestMethod]
        public void DeleteLimit()
        {
            using var q = NewQuery();
            q.Limit = 5;
            var g = new FbGrammar(q);

            Assert.ThrowsException<NotSupportedException>(() => g.Delete());
        }

        [TestMethod]
        public void DeleteOrder()
        {
            using var q = NewQuery();
            q.OrderBy("id");
            var g = new FbGrammar(q);

            Assert.ThrowsException<NotSupportedException>(() => g.Delete());
        }

        [TestMethod]
        public void DeleteWhere()
        {
            using var q = NewQuery();
            q.Where("id", "=", 1);
            var g = new FbGrammar(q);

            var sqlExpression = g.Delete();
            TestAssert.AreDecoded("DELETE FROM TestTable WHERE id = 1", sqlExpression);
        }

        [TestMethod]
        public void DeleteWhereJoin()
        {
            using var q = NewQuery(TABLE, "t1");
            q.Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            q.Where("t2.Id", 1);
            var g = new FbGrammar(q);

            Assert.ThrowsException<NotSupportedException>(() => g.Delete());
        }

        [TestMethod]
        public void DeleteJoins()
        {
            using var q = NewQuery(TABLE, "t1");
            q.JoinToDelete("t2").Join("Table2 t2", "t2.Id", "=", "t1.T2Id");
            q.Where("t2.Id", 1);
            var g = new FbGrammar(q);

           Assert.ThrowsException<NotSupportedException>(() => g.Delete());
        }

        [TestMethod]
        public void InsertByBasicSelect()
        {
            using var selectQuery = NewQuery("User");
            selectQuery
                .Select(new Column("Id"), (Column)"1")
                .Where("id", 1);

            using var q = NewQuery();
            var g = new FbGrammar(q);

            var sqlExpression = g.InsertQuery(selectQuery, new[] { "UserId", "Status" });
            TestAssert.AreDecoded("INSERT INTO TestTable (UserId, Status) SELECT Id, 1 FROM User WHERE id = 1", sqlExpression);
        }
    }
}

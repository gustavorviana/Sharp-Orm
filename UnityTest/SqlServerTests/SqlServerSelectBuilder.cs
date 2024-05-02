using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest.SqlServerTests
{
    [TestClass]
    public class SqlServerSelectBuilder : SqlServerTest
    {
        [TestMethod]
        public void FixColumnName()
        {
            var config = new SqlServerQueryConfig(false);
            string basic = config.ApplyNomenclature("colName");
            string withTable = config.ApplyNomenclature("table.colName");
            string all = config.ApplyNomenclature("*");
            string allWithTable = config.ApplyNomenclature("table.*");

            Assert.AreEqual("[colName]", basic);
            Assert.AreEqual("[table].[colName]", withTable);
            Assert.AreEqual("*", all);
            Assert.AreEqual("[table].*", allWithTable);
        }

        [TestMethod]
        public void Select()
        {
            using var query = NewQuery(TABLE);
            var g = NewConfig.NewGrammar(query);

            TestAssert.AreDecoded("SELECT * FROM [TestTable]", g.Select());
        }

        [TestMethod]
        public void SelectAndOrderBy()
        {
            using var query = NewQuery(TABLE);
            var g = NewConfig.NewGrammar(query.OrderBy("Id"));

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM [TestTable] ORDER BY [Id] Asc", sqlExpression);
        }

        [TestMethod]
        public void SelectOffset()
        {
            using var query = NewQuery(TABLE, "", OldConfig);
            query.OrderBy("Id").Offset = 1;
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Asc) AS [grammar_rownum], * FROM [TestTable]) [TestTable] WHERE [grammar_rownum] > 1", sqlExpression);
        }

        [TestMethod]
        public void SelectOffsetWhere()
        {
            using var query = NewQuery(TABLE, "", OldConfig);
            query.OrderBy("Id").Offset = 1;
            query.Where("Id", 1);
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Asc) AS [grammar_rownum], * FROM [TestTable] WHERE [Id] = 1) [TestTable] WHERE [grammar_rownum] > 1", sqlExpression);
        }

        [TestMethod]
        public void NewSelectOffset()
        {
            using var query = new Query(TABLE, GetConnectionManager());
            query.OrderBy("Id").Offset = 1;
            query.Where("Id", 1);
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM [TestTable] WHERE [Id] = 1 ORDER BY [Id] Asc OFFSET 1 ROWS", sqlExpression);
        }

        [TestMethod]
        public void NewSelectOffsetLimit()
        {
            using var query = new Query(TABLE, GetConnectionManager());
            query.OrderBy("Id");
            query.Where("Id", 1);
            query.Offset = 1;
            query.Limit = 10;
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM [TestTable] WHERE [Id] = 1 ORDER BY [Id] Asc OFFSET 1 ROWS FETCH NEXT 10 ROWS ONLY", sqlExpression);
        }

        [TestMethod]
        public void SelectLimitWhere()
        {
            using var query = NewQuery(TABLE);
            query.OrderBy("Id").Limit = 1;
            query.Where("Id", 1);
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT TOP(1) * FROM [TestTable] WHERE [Id] = 1 ORDER BY [Id] Asc", sqlExpression);
        }

        [TestMethod]
        public void SelectLimit()
        {
            using var query = NewQuery(TABLE);
            query.Limit = 1;
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT TOP(1) * FROM [TestTable]", sqlExpression);
        }

        [TestMethod]
        public void SelectWhere()
        {
            using var query = NewQuery(TABLE);
            query.Where("Id", 1);
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM [TestTable] WHERE [Id] = 1", sqlExpression);
        }

        [TestMethod]
        public void SelectAndPaginate()
        {
            using var query = NewQuery(TABLE, "table", OldConfig);
            query.OrderByDesc("Id").Select("Id", "Name");
            query.Offset = 1;
            query.Limit = 10;
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Desc) AS [grammar_rownum], [Id], [Name] FROM [TestTable] [table]) [table] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereAndPaginate()
        {
            using var query = NewQuery(TABLE, "", OldConfig);
            query.OrderByDesc("Id").Select("Id", "Name");
            query.Offset = 1;
            query.Limit = 10;
            query.Where("id", 1);
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Desc) AS [grammar_rownum], [Id], [Name] FROM [TestTable] WHERE [id] = 1) [TestTable] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

        [TestMethod]
        public void SelectGroupByPaginate()
        {
            using var query = NewQuery("Customer", "", OldConfig);
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]");
            query.Offset = 1;
            query.Limit = 10;

            var g = NewConfig.NewGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] Asc) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

        [TestMethod]
        public void SelectWhereGroupByPaginate()
        {
            using var query = NewQuery("Customer", "", OldConfig);
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]").Where("Id", "!=", 10);
            query.Offset = 1;
            query.Limit = 10;

            var g = NewConfig.NewGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] Asc) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] WHERE [Id] != 10 GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

        [TestMethod]
        public void CountDistinctSelect2()
        {
            using var query = new Query(TABLE, GetConnectionManager());
            query.Select("Column").Distinct = true;
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(DISTINCT [Column]) FROM [TestTable]", sqlExpression);
        }

        [TestMethod]
        public void CountDistinctSelect3()
        {
            using var query = new Query(TABLE, GetConnectionManager());
            query.Select("nick", "name").Distinct = true;
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM (SELECT DISTINCT [nick], [name] FROM [TestTable]) AS [count]", sqlExpression);
        }

        [TestMethod]
        public void SelectGroupByPaginateInnerJoin()
        {
            using var query = NewQuery("Customer", "", OldConfig);
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]").Where("Id", "!=", 10);
            query.Join("User", "User.Id", "=", "Customer.UserId");
            query.Offset = 1;
            query.Limit = 10;

            var g = NewConfig.NewGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] Asc) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] INNER JOIN [User] ON [User].[Id] = [Customer].[UserId] WHERE [Id] != 10 GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", sqlExpression);
        }

        [TestMethod]
        public void CountSelect()
        {
            using var query = new Query(TABLE, GetConnectionManager());
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM [TestTable]", sqlExpression);
        }

        [TestMethod]
        public void CountWhereSelect()
        {
            using var query = new Query(TABLE, GetConnectionManager());
            query.Where("Column", null);
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM [TestTable] WHERE [Column] IS NULL", sqlExpression);
        }

        [TestMethod]
        public void SelectJoin()
        {
            using var query = new Query(TABLE, GetConnectionManager());
            query.Join("Table2 t2", q => q.WhereColumn("t2.IdTable", "TestTable.Id"), grammarOptions: new SqlServerGrammarOptions { NoLock = true });
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM [TestTable] INNER JOIN [Table2] [t2] WITH (NOLOCK) ON [t2].[IdTable] = [TestTable].[Id]", sqlExpression);
        }

        [TestMethod]
        public void CountSelectJoin()
        {
            using var query = new Query(TABLE, GetConnectionManager());
            query
                .Join("Table2 t2", "t2.IdTable", "=", "TestTable.Id")
                .Where("t2.Column", "Value");
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM [TestTable] INNER JOIN [Table2] [t2] ON [t2].[IdTable] = [TestTable].[Id] WHERE [t2].[Column] = @p1", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void CountNewSelectJoin()
        {
            using var query = new Query(TABLE, GetConnectionManager());
            query
                .Join("Table2 t2", "t2.IdTable", "=", "TestTable.Id")
                .Where("t2.Column", "Value");
            var g = NewConfig.NewGrammar(query);

            var sqlExpression = g.Count();
            TestAssert.AreDecoded("SELECT COUNT(*) FROM [TestTable] INNER JOIN [Table2] [t2] ON [t2].[IdTable] = [TestTable].[Id] WHERE [t2].[Column] = @p1", sqlExpression);
            TestAssert.AreEqualsParameters(sqlExpression, 0);
        }

        [TestMethod]
        public void DeleteWithNoLock()
        {
            using var q = new Query(new DbName(TABLE, "T"), this.Creator);
            q.EnableNoLock();
            var g = NewConfig.NewGrammar(q);

            TestAssert.AreDecoded("DELETE [T] FROM [TestTable] [T] WITH (NOLOCK)", g.Delete());
        }

        [TestMethod]
        public void SelectWithEscapeStrings()
        {
            var today = DateTime.Today;
            using var query = new Query(TABLE, this.GetConnectionManager(EscapeStringsConfig));
            query.Where("Name", "Mike").Where("Date", today).Where("Alias", "\"Mik\";'Mik'#--");

            var g = EscapeStringsConfig.NewGrammar(query);
            var sqlExpression = g.Select();
            TestAssert.AreDecoded("SELECT * FROM [TestTable] WHERE [Name] = 'Mike' AND [Date] = @p1 AND [Alias] = '\"Mik\";''Mik''#--'", sqlExpression);
        }
    }
}

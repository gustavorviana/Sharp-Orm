using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.Data.Common;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest
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
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM [TestTable]", cmd.CommandText);
        }

        [TestMethod]
        public void SelectAndOrderBy()
        {
            using var query = NewQuery(TABLE);
            using var g = NewConfig.NewGrammar(query.OrderBy("Id"));

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM [TestTable] ORDER BY [Id] Asc", cmd.CommandText);
        }

        [TestMethod]
        public void SelectOffset()
        {
            using var query = NewQuery(TABLE, "", false);
            query.OrderBy("Id").Offset = 1;
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Asc) AS [grammar_rownum], * FROM [TestTable]) [TestTable] WHERE [grammar_rownum] > 1", cmd.CommandText);
        }

        [TestMethod]
        public void SelectOffsetWhere()
        {
            using var query = NewQuery(TABLE, "", false);
            query.OrderBy("Id").Offset = 1;
            query.Where("Id", 1);
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Asc) AS [grammar_rownum], * FROM [TestTable] WHERE [Id] = 1) [TestTable] WHERE [grammar_rownum] > 1", cmd.CommandText);
        }

        [TestMethod]
        public void NewSelectOffset()
        {
            using var query = new Query(Connection, new SqlServerQueryConfig(false), TABLE);
            query.OrderBy("Id").Offset = 1;
            query.Where("Id", 1);
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM [TestTable] WHERE [Id] = 1 ORDER BY [Id] Asc OFFSET 1 ROWS", cmd.CommandText);
        }

        [TestMethod]
        public void NewSelectOffsetLimit()
        {
            using var query = new Query(Connection, new SqlServerQueryConfig(false), TABLE);
            query.OrderBy("Id");
            query.Where("Id", 1);
            query.Offset = 1;
            query.Limit = 10;
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM [TestTable] WHERE [Id] = 1 ORDER BY [Id] Asc OFFSET 1 ROWS FETCH NEXT 10 ROWS ONLY", cmd.CommandText);
        }

        [TestMethod]
        public void SelectLimitWhere()
        {
            using var query = NewQuery(TABLE);
            query.OrderBy("Id").Limit = 1;
            query.Where("Id", 1);
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT TOP(1) * FROM [TestTable] WHERE [Id] = 1 ORDER BY [Id] Asc", cmd.CommandText);
        }

        [TestMethod]
        public void SelectLimit()
        {
            using var query = NewQuery(TABLE);
            query.Limit = 1;
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT TOP(1) * FROM [TestTable]", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhere()
        {
            using var query = NewQuery(TABLE);
            query.Where("Id", 1);
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM [TestTable] WHERE [Id] = 1", cmd.CommandText);
        }

        [TestMethod]
        public void SelectAndPaginate()
        {
            using var query = NewQuery(TABLE, "table", false);
            query.OrderByDesc("Id").Select("Id", "Name");
            query.Offset = 1;
            query.Limit = 10;
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Desc) AS [grammar_rownum], [Id], [Name] FROM [TestTable] [table]) [table] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereAndPaginate()
        {
            using var query = NewQuery(TABLE, "", false);
            query.OrderByDesc("Id").Select("Id", "Name");
            query.Offset = 1;
            query.Limit = 10;
            query.Where("id", 1);
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Desc) AS [grammar_rownum], [Id], [Name] FROM [TestTable] WHERE [id] = 1) [TestTable] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
        }

        [TestMethod]
        public void SelectGroupByPaginate()
        {
            using var query = NewQuery("Customer", "", false);
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]");
            query.Offset = 1;
            query.Limit = 10;

            using var g = NewConfig.NewGrammar(query);
            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] Asc) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereGroupByPaginate()
        {
            using var query = NewQuery("Customer", "", false);
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]").Where("Id", "!=", 10);
            query.Offset = 1;
            query.Limit = 10;

            using var g = NewConfig.NewGrammar(query);
            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] Asc) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] WHERE [Id] != 10 GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
        }

        [TestMethod]
        public void CountDistinctSelect2()
        {
            using var query = new Query(Connection, TABLE);
            query.Select("Column").Distinct = true;
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Count();
            Assert.AreEqual("SELECT COUNT(DISTINCT [Column]) FROM [TestTable]", cmd.CommandText);
        }

        [TestMethod]
        public void CountDistinctSelect3()
        {
            using var query = new Query(Connection, TABLE);
            query.Select("nick", "name").Distinct = true;
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Count();
            Assert.AreEqual("SELECT COUNT(*) FROM (SELECT DISTINCT [nick], [name] FROM [TestTable]) AS [count]", cmd.CommandText);
        }

        [TestMethod]
        public void SelectGroupByPaginateInnerJoin()
        {
            using var query = NewQuery("Customer", "", false);
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]").Where("Id", "!=", 10);
            query.Join("User", "User.Id", "=", "Customer.UserId");
            query.Offset = 1;
            query.Limit = 10;

            using var g = NewConfig.NewGrammar(query);
            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] Asc) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] INNER JOIN [User] ON [User].[Id] = [Customer].[UserId] WHERE [Id] != 10 GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
        }

        [TestMethod]
        public void CountSelect()
        {
            using var query = new Query(Connection, TABLE);
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Count();
            Assert.AreEqual("SELECT COUNT(*) FROM [TestTable]", cmd.CommandText);
        }

        [TestMethod]
        public void CountWhereSelect()
        {
            using var query = new Query(Connection, TABLE);
            query.Where("Column", null);
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Count();
            Assert.AreEqual("SELECT COUNT(*) FROM [TestTable] WHERE [Column] IS NULL", cmd.CommandText);
        }

        [TestMethod]
        public void SelectJoin()
        {
            using var query = new Query(Connection, TABLE);
            query.Join("Table2 t2", q => q.WhereColumn("t2.IdTable", "TestTable.Id"), grammarOptions: new SqlServerGrammarOptions { NoLock = true });
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM [TestTable] INNER JOIN [Table2] [t2] WITH (NOLOCK) ON [t2].[IdTable] = [TestTable].[Id]", cmd.CommandText);
        }

        [TestMethod]
        public void CountSelectJoin()
        {
            using var query = new Query(Connection, TABLE);
            query
                .Join("Table2 t2", "t2.IdTable", "=", "TestTable.Id")
                .Where("t2.Column", "Value");
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Count();
            Assert.AreEqual("SELECT COUNT(*) FROM [TestTable] INNER JOIN [Table2] [t2] ON [t2].[IdTable] = [TestTable].[Id] WHERE [t2].[Column] = @p1", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@p1", "Value");
        }

        [TestMethod]
        public void CountNewSelectJoin()
        {
            using var query = new Query(Connection, new SqlServerQueryConfig { UseOldPagination = false }, TABLE);
            query
                .Join("Table2 t2", "t2.IdTable", "=", "TestTable.Id")
                .Where("t2.Column", "Value");
            using var g = NewConfig.NewGrammar(query);

            using var cmd = g.Count();
            Assert.AreEqual("SELECT COUNT(*) FROM [TestTable] INNER JOIN [Table2] [t2] ON [t2].[IdTable] = [TestTable].[Id] WHERE [t2].[Column] = @p1", cmd.CommandText);
            AreEqualsParameter(cmd.Parameters[0], "@p1", "Value");
        }

        [TestMethod]
        public void DeleteWithNoLock()
        {
            using var q = new Query(Connection, new SqlServerQueryConfig(false), new DbName(TABLE, "T"));
            q.EnableNoLock();
            using var g = NewConfig.NewGrammar(q);

            using var cmd = g.Delete();
            Assert.AreEqual("DELETE [T] FROM [TestTable] [T] WITH (NOLOCK)", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWithEscapeStrings()
        {
            var today = DateTime.Today;
            using var query = new Query(Connection, EscapeStringsConfig, TABLE);
            query.Where("Name", "Mike").Where("Date", today).Where("Alias", "\"Mik\";'Mik'#--");

            using var g = EscapeStringsConfig.NewGrammar(query);
            using var cmd = g.Select();
            Assert.AreEqual("SELECT * FROM [TestTable] WHERE [Name] = 'Mike' AND [Date] = @p1 AND [Alias] = '\"Mik\";''Mik''#--'", cmd.CommandText);
        }

        private static void AreEqualsParameter(DbParameter param, string name, object value)
        {
            Assert.AreEqual(name, param.ParameterName);
            Assert.AreEqual(value, param.Value);
        }
    }
}

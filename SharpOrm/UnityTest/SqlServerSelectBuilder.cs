using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class SqlServerSelectBuilder : SqlServerTest
    {
        [TestMethod]
        public void Select()
        {
            using var query = NewQuery(TABLE);
            using var g = config.NewGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM [TestTable]", cmd.CommandText);
        }

        [TestMethod]
        public void SelectAndOrderBy()
        {
            using var query = NewQuery(TABLE);
            using var g = config.NewGrammar(query.OrderBy("Id"));

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM [TestTable] ORDER BY [Id] Asc", cmd.CommandText);
        }

        [TestMethod]
        public void SelectOffset()
        {
            using var query = NewQuery(TABLE);
            query.OrderBy("Id").Offset = 1;
            using var g = config.NewGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Asc) AS [grammar_rownum], * FROM [TestTable]) [TestTable] WHERE [grammar_rownum] > 1", cmd.CommandText);
        }

        [TestMethod]
        public void SelectLimit()
        {
            using var query = NewQuery(TABLE);
            query.Limit = 1;
            using var g = config.NewGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT TOP (1) * FROM [TestTable]", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhere()
        {
            using var query = NewQuery(TABLE);
            query.Where("Id", 1);
            using var g = config.NewGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM [TestTable] WHERE [Id] = @c1", cmd.CommandText);
            this.AreEqualsParameter(cmd.Parameters[0], "@c1", 1);
        }

        [TestMethod]
        public void SelectAndPaginate()
        {
            using var query = NewQuery(TABLE, "table");
            query.OrderByDesc("Id").Select("Id", "Name");
            query.Offset = 1;
            query.Limit = 10;
            using var g = config.NewGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [table].[Id] Desc) AS [grammar_rownum], [table].[Id], [table].[Name] FROM [TestTable] [table]) [table] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
        }

        private void AreEqualsParameter(DbParameter param, string name, object value)
        {
            Assert.AreEqual(name, param.ParameterName);
            Assert.AreEqual(value, param.Value);
        }
    }
}

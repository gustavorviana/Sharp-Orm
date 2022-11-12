﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
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
        public void SelectOffsetWhere()
        {
            using var query = NewQuery(TABLE);
            query.OrderBy("Id").Offset = 1;
            query.Where("Id", 1);
            using var g = config.NewGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Asc) AS [grammar_rownum], * FROM [TestTable] WHERE [Id] = @c1) [TestTable] WHERE [grammar_rownum] > 1", cmd.CommandText);
            this.AreEqualsParameter(cmd.Parameters[0], "@c1", 1);
        }

        [TestMethod]
        public void SelectLimitWhere()
        {
            using var query = NewQuery(TABLE);
            query.OrderBy("Id").Limit = 1;
            query.Where("Id", 1);
            using var g = config.NewGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT TOP (1) * FROM [TestTable] WHERE [Id] = @c1 ORDER BY [Id] Asc", cmd.CommandText);
            this.AreEqualsParameter(cmd.Parameters[0], "@c1", 1);
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

        [TestMethod]
        public void SelectWhereAndPaginate()
        {
            using var query = NewQuery(TABLE);
            query.OrderByDesc("Id").Select("Id", "Name");
            query.Offset = 1;
            query.Limit = 10;
            query.Where("id", 1);
            using var g = config.NewGrammar(query);

            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [Id] Desc) AS [grammar_rownum], [Id], [Name] FROM [TestTable] WHERE [id] = @c1) [TestTable] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
            this.AreEqualsParameter(cmd.Parameters[0], "@c1", 1);
        }

        [TestMethod]
        public void SelectGroupByPaginate()
        {
            using var query = NewQuery("Customer");
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]");
            query.Offset = 1;
            query.Limit = 10;

            using var g = config.NewGrammar(query);
            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] Asc) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
        }

        [TestMethod]
        public void SelectWhereGroupByPaginate()
        {
            using var query = NewQuery("Customer");
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]").Where("Id", "!=", 10);
            query.Offset = 1;
            query.Limit = 10;

            using var g = config.NewGrammar(query);
            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] Asc) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] WHERE [Id] != @c1 GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
            this.AreEqualsParameter(cmd.Parameters[0], "@c1", 10);
        }

        [TestMethod]
        public void SelectGroupByPaginateInnerJoin()
        {
            using var query = NewQuery("Customer");
            query.OrderBy("State").GroupBy("State").Select((Column)"State", (Column)"COUNT([State]) as [Count]").Where("Id", "!=", 10);
            query.Join("User", "User.Id", "=", "Customer.UserId");
            query.Offset = 1;
            query.Limit = 10;

            using var g = config.NewGrammar(query);
            using var cmd = g.GetSelectCommand();
            Assert.AreEqual("SELECT * FROM (SELECT ROW_NUMBER() OVER(ORDER BY [State] Asc) AS [grammar_rownum], State, COUNT([State]) as [Count] FROM [Customer] INNER JOIN [User] ON [User].[Id] = [Customer].[UserId] WHERE [Id] != @c1 GROUP BY [State]) [Customer] WHERE [grammar_rownum] BETWEEN 2 AND 11", cmd.CommandText);
            this.AreEqualsParameter(cmd.Parameters[0], "@c1", 10);
        }

        private void AreEqualsParameter(DbParameter param, string name, object value)
        {
            Assert.AreEqual(name, param.ParameterName);
            Assert.AreEqual(value, param.Value);
        }
    }
}
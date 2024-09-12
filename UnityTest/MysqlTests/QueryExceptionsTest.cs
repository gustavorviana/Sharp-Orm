using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using UnityTest.Utils;

namespace UnityTest.MysqlTests
{
    [TestClass]
    public class QueryExceptionsTest : MysqlTableTest
    {
        [TestMethod]
        public void OnCreateQueryWithNull()
        {
            ConnectionCreator.Default = null;
            Assert.ThrowsException<ArgumentNullException>(() => new Query("SomeTable"));
        }

        [TestMethod]
        public void OnQueryNullTable()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Query(null, this.Creator));
        }

        [TestMethod]
        public void OnQueryNullConfig()
        {
            using var con = this.Creator.GetConnection();
            ConnectionCreator.Default = this.Creator;
            Assert.ThrowsException<ArgumentNullException>(() => new Query("SomeTable", new ConnectionManager(null, con)));
        }

        [TestMethod]
        public void OnInvalidWhereOperation()
        {
            using var q = NewQuery();

            Assert.ThrowsException<ArgumentNullException>(() => q.Where("column", "", "value"));
            Assert.ThrowsException<ArgumentNullException>(() => q.Where("column", null, "value"));
            Assert.ThrowsException<DatabaseException>(() => q.Where("column", "None", "value"));
        }
    }
}

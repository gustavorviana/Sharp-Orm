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
            Assert.ThrowsException<ArgumentNullException>(() => new Query(Connection, null));
        }

        [TestMethod]
        public void OnQueryNullConfig()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Query(Connection, null, "SomeTable"));
        }

        [TestMethod]
        public void OnInvalidWhereOperation()
        {
            using var q = NewQuery();

            Assert.ThrowsException<DatabaseException>(() => q.Where("column", "", "value"));
            Assert.ThrowsException<NullReferenceException>(() => q.Where("column", null, "value"));
            Assert.ThrowsException<DatabaseException>(() => q.Where("column", "None", "value"));
        }
    }
}

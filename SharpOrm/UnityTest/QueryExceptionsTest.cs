using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Errors;
using System;
using Teste.Utils;

namespace UnityTest
{
    [TestClass]
    public class QueryExceptionsTest : MysqlTableTest
    {
        [TestMethod]
        public void OnCreateQueryWithNull()
        {
            QueryDefaults.Connection = null;
            Assert.ThrowsException<ArgumentNullException>(() => new Query("SomeTable"));
        }

        [TestMethod]
        public void OnQueryNullTable()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Query(connection, null));
        }

        [TestMethod]
        public void OnQueryNullConfig()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Query(connection, null, "SomeTable"));
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

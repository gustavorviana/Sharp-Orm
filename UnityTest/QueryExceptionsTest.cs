using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Connection;
using SharpOrm.Errors;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class QueryExceptionsTest : MysqlTableTest
    {
        [TestMethod]
        public void InvalidClassColumn()
        {
            using var q = new Query<InvalidTestClass>(Connection);
            q.Insert(NewRow(1, "1").Cells);

            Assert.ThrowsException<DatabaseException>(() => q.FirstOrDefault());
        }

        [TestMethod]
        public void OnCreateQueryWithNull()
        {
            ConnectionCreator.Default = null;
            Assert.ThrowsException<NullReferenceException>(() => new Query("SomeTable"));
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

        [Table(TABLE)]
        private class InvalidTestClass
        {
            [Key]
            public int MyId { get; set; }
            public string InvalidName { get; set; }
        }
    }
}

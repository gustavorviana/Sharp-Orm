﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System;
using System.Data.SqlClient;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class SqlServeCommandRunTest : SqlServerTest
    {
        protected static readonly SqlServerQueryConfig newConfig = new(false) { UseOldPagination = false };
        protected static readonly ConnectionCreator Creator = new SingleConnectionCreator<SqlConnection>(newConfig, ConnectionStr.SqlServer);

        [TestMethod]
        public void PaginateWithoutOrderby()
        {
            var q = new Query<TestTable>(Creator);
            Assert.ThrowsException<InvalidOperationException>(() => q.Paginate(1, 2));
        }

        [TestMethod]
        [TestProperty("clearDb", "")]
        public void Paginate()
        {
            InsertRows(30);

            var q = new Query<TestTable>(Creator);
            q.OrderBy("Id");
            var r = q.Paginate(5, 1);

            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Count);
            Assert.AreEqual(30, r.Total);
            Assert.AreEqual(1, r.CurrentPage);
        }

        [TestMethod]
        [TestProperty("clearDb", "")]
        public void PaginatePage2()
        {
            InsertRows(10);

            var q = new Query<TestTable>(Creator);
            q.OrderBy("Id");
            var r = q.Paginate(8, 2);

            Assert.IsNotNull(r);
            Assert.AreEqual(2, r.Count);
            Assert.AreEqual(10, r.Total);
            Assert.AreEqual(2, r.CurrentPage);
        }
    }
}
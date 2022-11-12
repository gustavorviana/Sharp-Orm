﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Errors;
using UnityTest.Utils;

namespace UnityTest.ConnectionTest
{
    [TestClass]
    public class MysqlTransactionTest : MysqlTableTest
    {
        [TestMethod]
        public void SimpleSelect()
        {
            try
            {
                QueryDefaults.ExecuteTransaction(() =>
                {
                    using var q = new Query(TABLE);
                    q.Insert(NewRow(1, "User 1").Cells);

                    using var qSelect = new Query(TABLE);

                    Assert.AreEqual(1, qSelect.Count());
                    throw new DatabaseException();
                });
            }
            catch (DatabaseException)
            {
                using var qSelect = new Query(TABLE);

                Assert.AreEqual(0, qSelect.Count());
            }
        }

        [TestMethod]
        public void MultipleInsert()
        {
            QueryDefaults.ExecuteTransaction(() =>
            {
                using var q = new Query(TABLE);
                q.Insert(NewRow(1, "User 1").Cells);
                q.Insert(NewRow(2, "User 2").Cells);
                q.Insert(NewRow(3, "User 3").Cells);
                q.Insert(NewRow(4, "User 4").Cells);

                Assert.AreEqual(4, q.Count());
            });

            using var q = new Query(TABLE);
            Assert.AreEqual(4, q.Count());
        }

        [TestCleanup]
        [TestInitialize]
        public void CleanupTest()
        {
            using var query = NewQuery();
            query.Delete();
        }
    }
}
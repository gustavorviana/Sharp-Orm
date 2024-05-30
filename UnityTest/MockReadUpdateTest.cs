using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityTest.Models;
using UnityTest.Utils;
using UnityTest.Utils.Mock;

namespace UnityTest
{
    [TestClass]
    public class MockReadUpdateTest : MockTest
    {
        [TestMethod]
        public void ReadWithCreateForeignIfNoDepth()
        {
            using var query = GetConfiguredOrderQuery(true);
            var order = query.FirstOrDefault();

            Assert.IsNotNull(order.Customer);

            Assert.AreNotEqual<uint>(0, order.CustomerId);
            Assert.AreEqual(order.CustomerId, order.Customer.Id);

            Assert.IsNull(order.Customer.Address);
        }

        [TestMethod]
        public void ReadDbNull()
        {
            var reader = new MockDataReader(new Cell("str", DBNull.Value), new Cell("num", null));
            reader.Read();

            Assert.AreEqual(DBNull.Value, reader[0]);
            Assert.AreEqual(DBNull.Value, reader[1]);

            Assert.AreEqual(null, reader.GetCell(TranslationRegistry.Default, 0).Value);
            Assert.AreEqual(null, reader.GetCell(TranslationRegistry.Default, 1).Value);
        }

        [TestMethod]
        public void Read100Itens()
        {
            const int itens = 1000;
            using var query = GetConfiguredOrderQuery(itens: itens, queryStr: "SELECT * FROM `Orders`");
            var orders = query.Get();

            Assert.AreEqual(itens, orders.Length);
        }

        private static Query<Order> GetConfiguredOrderQuery(bool loadForeign = false, int itens = 1, string queryStr = "SELECT * FROM `Orders` LIMIT 1")
        {
            var config = new MysqlQueryConfig { LoadForeign = loadForeign };
            var query = new Query<Order>(GetConnectionManager(config));
            Connection.QueryReaders.Add(queryStr, () => OrderReader(itens));
            return query;
        }

        [TestMethod]
        public void TestCancellationToken()
        {
            var src = new CancellationTokenSource();
            Connection.QueryReaders.Add("SELECT * FROM `Orders` LIMIT 1", () =>
            {
                Task.Run(async () =>
                {
                    await Task.Delay(5);
                    src.Cancel();
                });

                MockDataReader reader = OrderReader(1);
                reader.ReadDelay = 500;
                reader.Token = src.Token;
                return reader;
            });

            using var query = new Query<Order>(GetConnectionManager()) { Token = src.Token };
            Assert.ThrowsException<OperationCanceledException>(() => query.FirstOrDefault());
        }

        [TestMethod]
        public void ReadObjectPk()
        {
            var info = new TableInfo(typeof(Order));
            var obj = new Order { Id = 1 };
            Assert.IsNotNull(info.GetObjCells(obj, true, false).FirstOrDefault(c => c.Name.Equals("id", StringComparison.CurrentCultureIgnoreCase)));
        }

        [TestMethod]
        public void ReadObjectIgnorePk()
        {
            var info = new TableInfo(typeof(Order));
            var obj = new Order { Id = 1 };
            Assert.IsNull(info.GetObjCells(obj, false, false).FirstOrDefault(c => c.Name.Equals("id", StringComparison.CurrentCultureIgnoreCase)));
        }

        [TestMethod]
        public void ReadLot()
        {
            var array = (IEnumerable<int>)new int[] { 1, 2, 3, 4, 5 };
            using var enumerator = array.GetEnumerator();

            enumerator.MoveNext();
            CollectionAssert.AreEqual(new int[] { 1, 2 }, QueryExtension.GetPage(enumerator, 2).ToArray());

            enumerator.MoveNext();
            CollectionAssert.AreEqual(new int[] { 3, 4 }, QueryExtension.GetPage(enumerator, 2).ToArray());

            enumerator.MoveNext();
            CollectionAssert.AreEqual(new int[] { 5 }, QueryExtension.GetPage(enumerator, 2).ToArray());
        }

        [TestMethod]
        public void UpdateByPropName()
        {
            var config = new MysqlQueryConfig(false);
            var conn = GetNonQueryCommand("UPDATE `Orders` SET `Quantity` = 1");

            var query = new Query<Order>(GetConnectionManager(config));
            query.Update(new Order { Quantity = 1 }, o => o.Quantity);
        }

        [TestMethod]
        public void UpdateIgnorePropName()
        {
            var config = new MysqlQueryConfig(false);
            var conn = GetNonQueryCommand("UPDATE `Orders` SET `Quantity` = 1");

            var query = new Query<Order>(GetConnectionManager(config));
            query.UpdateExcept(new Order { Quantity = 1 }, o => o.Customer, o => o.CustomerId, o => o.Product, o => o.Status);
        }

        private static MockDataReader OrderReader(int qtd)
        {
            return GetReader(i => new Cell[] {
                new("Id", i + 1),
                new("customer_id", (uint)i + 1),
                new("Product", $"My product {i + 1}"),
                new("Quantity", i + 10),
                new("Status", "Pending")
            }, qtd);
        }
    }
}
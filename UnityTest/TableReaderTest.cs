using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityTest.Models;
using UnityTest.Utils;
using UnityTest.Utils.Mock;

namespace UnityTest
{
    [TestClass]
    public class TableReaderTest : MockTest
    {
        [TestMethod]
        public void ReadWithCreateForeignIfNoDepth()
        {
            using var query = GetConfiguredQuery<Order>(true);
            var order = query.FirstOrDefault();

            Assert.IsNotNull(order.Customer);

            Assert.AreNotEqual<uint>(0, order.CustomerId);
            Assert.AreEqual(order.CustomerId, order.Customer.Id);

            Assert.IsNull(order.Customer.Address);
        }

        [TestMethod]
        public void ReadWithOutCreateForeignIfNoDepth()
        {
            using var query = GetConfiguredQuery<Order>();
            var order = query.FirstOrDefault();

            Assert.IsNull(order.Customer);
        }

        private static Query<T> GetConfiguredQuery<T>(bool foreignLoader = false) where T : class, new()
        {
            var config = new MysqlQueryConfig { ForeignLoader = foreignLoader };
            var query = new Query<T>(Connection, config);
            Connection.QueryReaders.Add("SELECT * FROM `Orders` LIMIT 1", () => OrderReader(1));
            return query;
        }

        [TestMethod]
        public void TestCancellationToken()
        {
            var src = new CancellationTokenSource();
            Connection.QueryReaders.Add("SELECT * FROM `Orders` LIMIT 1", () =>
            {
                MockDataReader reader = OrderReader(1);
                reader.ReadDelay = 500;
                reader.Token = src.Token;
                return reader;
            });

            Task.Run(async () =>
            {
                await Task.Delay(10);
                src.Cancel();
            });

            using var query = new Query<Order>(Connection, Config) { Token = src.Token };
            Assert.ThrowsException<OperationCanceledException>(() => query.FirstOrDefault());
        }

        private static MockDataReader OrderReader(int qtd)
        {
            return GetReader(qtd, i => new Order
            {
                Id = i + 1,
                CustomerId = (uint)i + 1,
                Product = $"My product {i + 1}",
                Quantity = i + 10,
                Status = "Pending"
            }, true);
        }
    }
}
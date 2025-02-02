using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using Xunit.Abstractions;

namespace QueryTest.DataTranslation
{
    public class MockReadUpdateTests(ITestOutputHelper? output) : DbMockFallbackTest(output)
    {
        [Fact]
        public void ReadWithCreateForeignIfNoDepth()
        {
            using var query = GetConfiguredOrderQuery(true);
            var order = query.FirstOrDefault();

            Assert.NotNull(order.Customer);

            Assert.NotEqual<uint>(0, order.CustomerId);
            Assert.Equal(order.CustomerId, order.Customer.Id);

            Assert.Null(order.Customer.Address);
        }

        [Fact]
        public void ReadDbNull()
        {
            var reader = new MockDataReader(new Cell("str", DBNull.Value), new Cell("num", null));
            reader.Read();

            Assert.Equal(DBNull.Value, reader[0]);
            Assert.Equal(DBNull.Value, reader[1]);

            Assert.Null(reader.GetCell(TranslationRegistry.Default, 0).Value);
            Assert.Null(reader.GetCell(TranslationRegistry.Default, 1).Value);
        }

        [Fact]
        public void Read100Itens()
        {
            const int itens = 1000;
            using var query = GetConfiguredOrderQuery(itens: itens, queryStr: "SELECT * FROM [Orders]");
            var orders = query.Get();

            Assert.Equal(itens, orders.Length);
        }

        private Query<Order> GetConfiguredOrderQuery(bool loadForeign = false, int itens = 1, string queryStr = "SELECT TOP(1) * FROM [Orders]")
        {
            var config = Config.Clone();
            config.LoadForeign = loadForeign;

            var query = new Query<Order>(GetManager(config));
            Connection.QueryReaders.Add(queryStr, () => OrderReader(itens));
            return query;
        }

        [Fact]
        public void ReadObjectPk()
        {
            var info = Translation.GetTable(typeof(Order));
            var obj = new Order { Id = 1 };
            Assert.NotNull(info.GetObjCells(obj, true, false).FirstOrDefault(c => c.Name.Equals("id", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void ReadObjectIgnorePk()
        {
            var info = Translation.GetTable(typeof(Order));
            var obj = new Order { Id = 1 };
            Assert.Null(info.GetObjCells(obj, false, false).FirstOrDefault(c => c.Name.Equals("id", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public void ReadLot()
        {
            var array = (IEnumerable<int>)[1, 2, 3, 4, 5];
            using var enumerator = array.GetEnumerator();

            enumerator.MoveNext();
            Assert.Equal([1, 2], QueryExtension.GetPage(enumerator, 2).ToArray());

            enumerator.MoveNext();
            Assert.Equal([3, 4], QueryExtension.GetPage(enumerator, 2).ToArray());

            enumerator.MoveNext();
            Assert.Equal([5], QueryExtension.GetPage(enumerator, 2).ToArray());
        }

        [Fact]
        public void UpdateByPropName()
        {
            var config = new MysqlQueryConfig(false);
            var fallback = RegisterFallback();

            var query = new Query<Order>(GetManager(config));
            query.Update(new Order { Quantity = 1 }, o => o.Quantity);

            Assert.Equal("UPDATE `Orders` SET `Quantity` = 1", fallback.ToString());
        }

        [Fact]
        public void UpdateIgnorePropName()
        {
            var config = new MysqlQueryConfig(false);
            var fallback = RegisterFallback();

            var query = new Query<Order>(GetManager(config));
            query.UpdateExcept(new Order { Quantity = 1 }, o => new { o.Customer, o.CustomerId, o.Product, o.Status });

            Assert.Equal("UPDATE `Orders` SET `Quantity` = 1", fallback.ToString());
        }

        private static MockDataReader OrderReader(int qtd)
        {
            return GetReader(i => [
                new("Id", i + 1),
                new("customer_id", (uint)i + 1),
                new("Product", $"My product {i + 1}"),
                new("Quantity", i + 10),
                new("Status", "Pending")
            ], qtd);
        }
    }
}
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class TableReaderTest : MysqlTableTest
    {
        [TestMethod]
        public void ReadWithCreateForeignIfNoDepth()
        {
            MySqlCommandRunTest.ConfigureInitialCustomerAndOrder();

            using var query = new Query<Order>(NewCreator(true));
            var order = query.FirstOrDefault();

            Assert.IsNotNull(order.Customer);
            Assert.AreEqual(order.CustomerId, order.Customer.Id);

            Assert.IsNull(order.Customer.Address);
        }

        [TestMethod]
        public void ReadWithOutCreateForeignIfNoDepth()
        {
            MySqlCommandRunTest.ConfigureInitialCustomerAndOrder();

            using var query = new Query<Order>(NewCreator(false));
            var order = query.FirstOrDefault();

            Assert.IsNull(order.Customer);
        }

        private static ConnectionCreator NewCreator(bool createForeignIfNoDepth)
        {
            return new MultipleConnectionCreator<MySqlConnection>(new MysqlQueryConfig(false) { ForeignLoader = createForeignIfNoDepth }, ConnectionStr.Mysql);
        }
    }
}
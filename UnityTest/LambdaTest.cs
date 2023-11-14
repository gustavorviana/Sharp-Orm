using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm.Builder;
using System.Linq;
using System.Linq.Expressions;
using UnityTest.Models;

namespace UnityTest
{
    [TestClass]
    public class LambdaTest
    {
        private static readonly QueryInfo QInfo = new(new MysqlQueryConfig(), new DbName("name", "n"));

        [TestMethod]
        public void GetOneColumn()
        {
            var columns = ToColumn(c => c.Customer);

            Assert.AreEqual(1, columns.Length);
            Assert.AreEqual(typeof(Customer), columns[0].ValueType);
        }

        [TestMethod]
        public void GetMultiple()
        {
            var columns = ToColumn(c => c.Customer.Address);

            Assert.AreEqual(2, columns.Length);
            Assert.AreEqual(typeof(Customer), columns[0].ValueType);
            Assert.AreEqual(typeof(Address), columns[1].ValueType);
        }

        private static LambdaColumn[] ToColumn(Expression<ColumnExpression<Order>> check)
        {
            return new ColumnExpressionVisitor(QInfo).VisitColumn(check).ToArray();
        }
    }
}

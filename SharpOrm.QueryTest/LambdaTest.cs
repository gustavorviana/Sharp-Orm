using BaseTest.Models;
using SharpOrm.Builder;
using System.Linq.Expressions;

namespace UnityTest
{
    public class LambdaTest
    {
        [Fact]
        public void GetOneColumn()
        {
            var columns = ToColumn(c => c.Customer);

            Assert.Single(columns);
            Assert.Equal(typeof(Customer), columns[0].ValueType);
        }

        [Fact]
        public void GetMultiple()
        {
            var columns = ToColumn(c => c.Customer.Address);

            Assert.Equal(2, columns.Length);
            Assert.Equal(typeof(Customer), columns[0].ValueType);
            Assert.Equal(typeof(Address), columns[1].ValueType);
        }

        private static MemberInfoColumn[] ToColumn(Expression<ColumnExpression<Order>> check)
        {
            return ExpressionUtils<Order>.GetColumnPath(check).ToArray();
        }
    }
}

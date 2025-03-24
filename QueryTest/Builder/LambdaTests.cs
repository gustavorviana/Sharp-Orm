using BaseTest.Models;
using SharpOrm.Builder;
using System.Linq.Expressions;

namespace QueryTest.Builder
{
    public class LambdaTests
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
            var cols = new List<MemberInfoColumn>();

            foreach (var member in ExpressionUtils<Order>.GetMemberPath(check, false))
                cols.Insert(0, new MemberInfoColumn(member));

            return [.. cols];
        }
    }
}

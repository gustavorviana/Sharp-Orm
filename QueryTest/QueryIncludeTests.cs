using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.DataTranslation;

namespace QueryTest
{
    public class QueryIncludeTests : DbMockFallbackTest
    {
        private readonly Query<CustomerWithOrders> _query;

        public QueryIncludeTests()
        {
            _query = new Query<CustomerWithOrders>();
        }

        [Fact]
        public virtual void Include_Sould_Join_Test()
        {
            _query.Include(x => x.Address);

            Assert.Single(_query.Info.Joins);
            Assert.Equal("Address", _query.Info.Joins.First().Info.TableName.Name);
        }

        [Fact]
        public virtual void Include_Collection_Sould_Not_Join_Test()
        {
            _query.Include(x => x.Orders);
            Assert.Empty(_query.Info.Joins);
        }

        [Fact]
        public virtual void Repeated_Includes_Sould_One_Join_Test()
        {
            _query.Include(x => x.Address);
            _query.Include(x => x.Address)
                .Include(x => x.Address);

            Assert.Single(_query.Info.Joins);
            Assert.Equal("Address", _query.Info.Joins.First().Info.TableName.Name);
        }

        [Fact]
        public virtual void ThenInclude()
        {
            var register = ((IFkNodeRoot)_query).ForeignKeyRegister;

            _query.Include(x => x.Orders).ThenInclude(x => x.Customer);

            Assert.Empty(_query.Info.Joins);
            Assert.Single(register.Nodes);
            Assert.Single(register.Nodes.First().Nodes);
            Assert.Equal("Orders", register.Nodes.First().Name.Name);
            Assert.Equal("Customers", register.Nodes.First().Nodes.First().Name.Name);
        }

        [Fact]
        public virtual void Include_Result_Must_Be_Cached_Test()
        {
            var address1 = _query.Include(x => x.Address);
            var address2 = _query.Include(x => x.Address);
            var address3 = address2.Include(x => x.Address);

            var orders1 = _query.Include(x => x.Orders);
            var orders2 = _query.Include(x => x.Orders);

            Assert.Equal(orders1, orders2);
            Assert.Equal(address1, address2);
            Assert.Equal(address1, address3);
        }

        private class CustomerWithOrders : Customer
        {
            public virtual ICollection<Order> Orders { get; set; } = [];
        }
    }
}

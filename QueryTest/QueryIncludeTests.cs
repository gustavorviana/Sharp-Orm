using BaseTest.Models;
using BaseTest.Utils;
using Bogus;
using SharpOrm;
using SharpOrm.DataTranslation;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Fact]
        public void Include_Sub_Foreign_Keys_Test()
        {
            var fakeOrder = Tables.Order.Faker().Generate();
            fakeOrder.CustomerId = 10;

            var fakerCustomer = new Faker<Customer>()
                .RuleFor(x => x.Id, f => fakeOrder.CustomerId)
                .RuleFor(x => x.Name, f => f.Person.FullName)
                .RuleFor(x => x.Name, f => f.Person.FullName)
                .RuleFor(x => x.Email, f => f.Internet.Email());

            var customer = fakerCustomer.Generate();
            customer.AddressId = 1;

            var address = Tables.Address.Faker().Generate();

            Connection.QueryReaders.Add(
                "SELECT * FROM [Customers]",
                () => GetReader(Row.Parse(customer).Cells)
            );

            var builder = new RowBuilder()
                .AddObject(Row.Parse(fakeOrder))
                .AddObject("Customer_c_", customer)
                .AddObject("Customer_Address_c_", address);

            Connection.QueryReaders.Add(
                "SELECT [Orders].[Id], [Orders].[customer_id], [Orders].[Product], [Orders].[Quantity], [Orders].[Status], [Customer].[Id] AS [Customer_c_Id], [Customer].[Name] AS [Customer_c_Name], [Customer].[Email] AS [Customer_c_Email], [Customer].[address_id] AS [Customer_c_address_id], [Customer_Address].[Id] AS [Customer_Address_c_Id], [Customer_Address].[Name] AS [Customer_Address_c_Name], [Customer_Address].[Street] AS [Customer_Address_c_Street], [Customer_Address].[City] AS [Customer_Address_c_City] FROM [Orders] LEFT JOIN [Customers] [Customer] ON [Customer].[Id] = [Orders].[customer_id] WHERE [Orders].[customer_id] = 10",
                () => GetReader(builder.ToRow().Cells)
            );

            using var fallback = RegisterFallback();
            _query
                .Include(x => x.Orders)
                .ThenInclude(x => x.Customer)
                .ThenInclude(x => x.Address);

            var result = _query.Get();

            Assert.Empty(fallback.ToString());

            var mainCustomer = Assert.Single(result);
            var order = Assert.Single(mainCustomer.Orders);

            Assert.NotNull(order.Product);
            Assert.NotNull(order.Status);

            Assert.NotNull(order.Customer);
            Assert.NotNull(order.Customer.Name);
            Assert.NotNull(order.Customer.Email);
        }

        [Table("Customers")]
        private class CustomerWithOrders : Customer
        {
            [Foreign("id", LocalKey = "customer_id")]
            public virtual ICollection<Order> Orders { get; set; } = [];
        }
    }
}

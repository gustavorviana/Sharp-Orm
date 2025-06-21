using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.SqlMethods;
using Xunit.Abstractions;

namespace QueryTest
{
    public class QueryLambdaTest(ITestOutputHelper output, MockFixture<SqlServerQueryConfig> connection) : DbMockFallbackTest(output, connection), IClassFixture<MockFixture<SqlServerQueryConfig>>
    {
        [Fact]
        public void SelectWhereAddressColumnWithoutInclude_ThrowsError()
        {
            using var query = new Query<Order>();
            query.Where(c => c.Customer.Address!.Name, "Test");

            var exception = Assert.Throws<ForeignMemberException>(query.Get);
            Assert.Equal("It's not possible to load the 'Name' property because there is no include configured for the 'Address' table or for the 'BaseTest.Models.Address' type.", exception.Message);
        }

        [Fact]
        public void SelectAddressColumnWithoutInclude_ThrowsError()
        {
            using var query = new Query<Order>();
            query.Select(c => c.Customer.Address!.Name);

            var exception = Assert.Throws<ForeignMemberException>(query.Get);
            Assert.Equal("It's not possible to load the 'Name' property because there is no include configured for the 'Address' table or for the 'BaseTest.Models.Address' type.", exception.Message);
        }

        [Fact]
        public void SelectWhereColumnWithoutInclude_ThrowsError()
        {
            using var query = new Query<Order>();
            query.Where(c => c.Customer.Name, "Test");

            var exception = Assert.Throws<ForeignMemberException>(query.Get);
            Assert.Equal("It's not possible to load the 'Name' property because there is no include configured for the 'Customers' table or for the 'BaseTest.Models.Customer' type.", exception.Message);
        }

        [Fact]
        public void SelectColumnWithoutInclude_ThrowsError()
        {
            using var query = new Query<Order>();
            query.Select(c => c.Customer.Name);

            var exception = Assert.Throws<ForeignMemberException>(query.Get);
            Assert.Equal("It's not possible to load the 'Name' property because there is no include configured for the 'Customers' table or for the 'BaseTest.Models.Customer' type.", exception.Message);
        }

        [Fact]
        public void JoinWithLambda()
        {
            using var fallback = RegisterFallback();

            using var query = new Query<Order>();
            query.Join(x => x.Customer, x => x.Id, x => x.CustomerId);
            query.Get();

            Assert.Equal("SELECT [Orders].[Id], [Orders].[customer_id], [Orders].[Product], [Orders].[Quantity], [Orders].[Status], [Customer].[Id] AS [Customer_c_Id], [Customer].[Name] AS [Customer_c_Name], [Customer].[Email] AS [Customer_c_Email], [Customer].[address_id] AS [Customer_c_address_id] FROM [Orders] INNER JOIN [Customers] [Customer] ON [Customer].[Id] = [Orders].[customer_id]", fallback.ToString());
        }

        [Fact]
        public void IncludeWithLambda()
        {
            using var fallback = RegisterFallback();

            using var query = new Query<Order>();
            query.Include(x => x.Customer);
            query.Get();

            Assert.Equal("SELECT [Orders].[Id], [Orders].[customer_id], [Orders].[Product], [Orders].[Quantity], [Orders].[Status], [Customer].[Id] AS [Customer_c_Id], [Customer].[Name] AS [Customer_c_Name], [Customer].[Email] AS [Customer_c_Email], [Customer].[address_id] AS [Customer_c_address_id] FROM [Orders] LEFT JOIN [Customers] [Customer] ON [Customer].[Id] = [Orders].[customer_id]", fallback.ToString());
        }

        [Fact]
        public void SelectWhereColumnWithoutInclude()
        {
            using var fallback = RegisterFallback();
            using var query = new Query<Order>();

            query.Include(x => x.Customer);
            query.Where(c => c.Customer.Name, "Test");

            query.Get();
            Assert.Equal("SELECT [Orders].[Id], [Orders].[customer_id], [Orders].[Product], [Orders].[Quantity], [Orders].[Status], [Customer].[Id] AS [Customer_c_Id], [Customer].[Name] AS [Customer_c_Name], [Customer].[Email] AS [Customer_c_Email], [Customer].[address_id] AS [Customer_c_address_id] FROM [Orders] LEFT JOIN [Customers] [Customer] ON [Customer].[Id] = [Orders].[customer_id] WHERE [Customer].[Name] = @p1", fallback.ToString());
        }
    }
}

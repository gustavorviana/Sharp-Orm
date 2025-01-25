using BaseTest.Fixtures;
using BaseTest.Models;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.SqlMethods;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class QueryTWhereClauseTest(ITestOutputHelper output, MockFixture<SqlServerQueryConfig> connection) :
        DbGrammarTestBase(output, connection), IClassFixture<MockFixture<SqlServerQueryConfig>>
    {
        [Fact]
        public void SelectWithWhereContains()
        {
            using var query = new Query<Address>();
            query.WhereContains(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] LIKE @p1", ["%Main%"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithWhereStartsWith()
        {
            using var query = new Query<Address>();
            query.WhereStartsWith(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] LIKE @p1", ["Main%"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithWhereEndsWith()
        {
            using var query = new Query<Address>();
            query.WhereEndsWith(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] LIKE @p1", ["%Main"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithWhereNotContains()
        {
            using var query = new Query<Address>();
            query.WhereNotContains(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] NOT LIKE @p1", ["%Main%"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithWhereNotStartsWith()
        {
            using var query = new Query<Address>();
            query.WhereNotStartsWith(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] NOT LIKE @p1", ["Main%"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithWhereNotEndsWith()
        {
            using var query = new Query<Address>();
            query.WhereNotEndsWith(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] NOT LIKE @p1", ["%Main"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithWhereNot()
        {
            using var query = new Query<Address>();
            query.WhereNot(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] != @p1", ["Main"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithWhere()
        {
            using var query = new Query<Address>();
            query.Where(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] = @p1", ["Main"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithWhereColumn()
        {
            using var query = new Query<Address>();
            query.WhereColumn(x => x.Street, x => x.City);

            QueryAssert.Equal("SELECT * FROM [Address] WHERE [Street] = [City]", query.Grammar().Select());
        }

        [Fact]
        public void SelectWithWhereNotColumn()
        {
            using var query = new Query<Address>();
            query.WhereNotColumn(x => x.Street, x => x.City);

            QueryAssert.Equal("SELECT * FROM [Address] WHERE [Street] != [City]", query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhereContains()
        {
            using var query = new Query<Address>();
            query.OrWhereContains(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] LIKE @p1", ["%Main%"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhereStartsWith()
        {
            using var query = new Query<Address>();
            query.OrWhereStartsWith(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] LIKE @p1", ["Main%"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhereEndsWith()
        {
            using var query = new Query<Address>();
            query.OrWhereEndsWith(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] LIKE @p1", ["%Main"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhereNotContains()
        {
            using var query = new Query<Address>();
            query.OrWhereNotContains(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] NOT LIKE @p1", ["%Main%"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhereNotStartsWith()
        {
            using var query = new Query<Address>();
            query.OrWhereNotStartsWith(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] NOT LIKE @p1", ["Main%"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhereNotEndsWith()
        {
            using var query = new Query<Address>();
            query.OrWhereNotEndsWith(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] NOT LIKE @p1", ["%Main"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhereNot()
        {
            using var query = new Query<Address>();
            query.OrWhereNot(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] != @p1", ["Main"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhere()
        {
            using var query = new Query<Address>();
            query.OrWhere(x => x.Street, "Main");

            QueryAssert.EqualDecoded("SELECT * FROM [Address] WHERE [Street] = @p1", ["Main"], query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhereColumn()
        {
            using var query = new Query<Address>();
            query.OrWhereColumn(x => x.Street, x => x.City);

            QueryAssert.Equal("SELECT * FROM [Address] WHERE [Street] = [City]", query.Grammar().Select());
        }

        [Fact]
        public void SelectWithOrWhereNotColumn()
        {
            using var query = new Query<Address>();
            query.OrWhereNotColumn(x => x.Street, x => x.City);

            QueryAssert.Equal("SELECT * FROM [Address] WHERE [Street] != [City]", query.Grammar().Select());
        }

        [Fact]
        public void SelectWithJoinWhere()
        {
            using var query = new Query<Order>();
            query.Join<Customer>(x => x.AddressId, x => x.Id);
            query.Where(x => x.Product, "Test");
            QueryAssert.Equal("SELECT * FROM [Orders] INNER JOIN [Customers] ON [Customers].[address_id] = [Orders].[Id] WHERE [Orders].[Product] = ?", query.Grammar().Select());
        }

        [Fact]
        public void SelectLowerProductWithJoinWhere()
        {
            using var query = new Query<Order>();
            query.Join<Customer>(x => x.AddressId, x => x.Id);
            query.Where(x => x.Product.ToLower(), "test");

            QueryAssert.Equal("SELECT * FROM [Orders] INNER JOIN [Customers] ON [Customers].[address_id] = [Orders].[Id] WHERE LOWER([Orders].[Product]) = ?", query.Grammar().Select());
        }
    }
}
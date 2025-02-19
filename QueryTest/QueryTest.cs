using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Linq.Expressions;
using Xunit.Abstractions;

namespace QueryTest
{
    public class QueryTest(ITestOutputHelper? output) : DbMockFallbackTest(output)
    {
        [Fact]
        public void InsertT_ShouldNotChangeAddressId()
        {
            const int expectedId = 1;
            var query = new Query<Address>();
            var addr = new Address(1)
            {
                City = "City"
            };

            query.Insert(addr);
            Assert.Equal(expectedId, addr.Id);
        }

        [Fact]
        public void InsertT_ShouldNotChangeIdGuid()
        {
            var expectedId = Guid.NewGuid();
            var query = new Query<GuidIdModel>();
            var addr = new GuidIdModel
            {
                Id = expectedId,
                Value = "Value"
            };

            query.Insert(addr);
            Assert.Equal(expectedId, addr.Id);
        }

        [Fact]
        public void OrderBy_ShouldApplyAscendingOrder()
        {
            // Arrange
            var query = new Query<Address>();
            Expression<ColumnExpression<Address>> expression = x => x.City;

            // Act
            var result = query.OrderBy(expression);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.City) && o.Order == SharpOrm.OrderBy.Asc);
        }

        [Fact]
        public void OrderBy_SubstringIndex2_ShouldApplyAscendingOrder()
        {
            // Arrange
            var query = new Query<Address>();
            Expression<ColumnExpression<Address>> expression = x => x.City.Substring(x.Street.Length);

            // Act
            var result = query.OrderBy(expression);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(
                result.Info.Orders, o => o.Column.Name == nameof(Address.City) &&
                o.Order == SharpOrm.OrderBy.Asc &&
                o.Column.ToString(query, false) == "SUBSTRING([City],LEN([Street]))"
            );
        }

        [Fact]
        public void OrderBy()
        {
            var query = new Query("table");
            query.OrderBy(SharpOrm.OrderBy.None, "Col1");
            Assert.Empty(query.Info.Orders);

            query.OrderBy(SharpOrm.OrderBy.Asc, "Col2");
            Assert.Single(query.Info.Orders);

            query.OrderBy(SharpOrm.OrderBy.Desc, "3");
            Assert.Single(query.Info.Orders);
        }

        [Fact]
        public void WhereQuery()
        {
            var query = new Query("table");
            var toWhereQuery = Query.ReadOnly("ToWhereQuery");

            Assert.Throws<InvalidOperationException>(() => query.Where("Column", toWhereQuery));

            toWhereQuery.Select("Column");
            Assert.Throws<InvalidOperationException>(() => query.Where("Column", toWhereQuery));

            toWhereQuery.Limit = 1;
            query.Where("Column", toWhereQuery);
        }

        [Fact]
        public void WhereInQuery()
        {
            var query = new Query("table");
            var toWhereQuery = new Query("ToWhereQuery");

            Assert.Throws<InvalidOperationException>(() => query.WhereIn("Column", toWhereQuery));

            toWhereQuery.Select("Column");
            query.WhereIn("Column", toWhereQuery);
        }

        [Fact]
        public void Clone()
        {
            var token = new CancellationToken(true);
            var original = new Query("table alias")
            {
                Limit = 1,
                Offset = 3,
                Token = token,
                Distinct = true
            };

            original.OrderBy("Id");
            original.Select("Col1", "Col2");
            original.WhereColumn("Col1", "=", "Col2");

            Assert.Equal(original.ToString(), original.Clone(true).ToString());
            var clone = original.Clone(false);

            var cloneQuery = original.Clone(false);
            cloneQuery.OrderBy("Id");

            Assert.NotEqual(original.ToString(), cloneQuery.ToString());
            Assert.Equal(original.Limit, clone.Limit);
            Assert.Equal(original.Offset, clone.Offset);
            Assert.Equal(original.Distinct, clone.Distinct);
            Assert.Equal(token, clone.Token);
        }

        [Fact]
        public void DefaultTimeoutTest()
        {
            var query = new Query("table");

            using var cmd = query.GetCommand();
            Assert.Equal(30, cmd.Timeout);
        }

        [Fact]
        public void QueryCustomTimeoutTest()
        {
            var query = new Query("table")
            {
                CommandTimeout = 120
            };

            using var cmd = query.GetCommand();
            Assert.Equal(120, cmd.Timeout);
        }

        [Fact]
        public void ConfigCustomTimeoutTest()
        {
            using var creator = new MultipleConnectionCreator<MockConnection>(new SqlServerQueryConfig(false) { CommandTimeout = 120 }, "");
            var query = new Query("table", creator);

            using var cmd = query.GetCommand();
            Assert.Equal(120, cmd.Timeout);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InsertSetObjIdTest(bool applyGeneratedKey)
        {
            var order = Tables.Order.Faker(false).Generate();
            int id = new Random().Next(1, 150);

            using (RegisterFallback(new Cell("Id", id)))
            {
                var manager = this.GetManager(this.Config.Clone());
                manager.Config.ApplyGeneratedKey = applyGeneratedKey;

                using var query = new Query<Order>(manager);

                Assert.Equal(id, query.Insert(order));

                if (applyGeneratedKey) Assert.Equal(order.Id, order.Id);
                else Assert.Equal(0, order.Id);
            }
        }

        [Fact]
        public void InsertWithNullResponse()
        {
            var order = Tables.Order.Faker(false).Generate();

            using (RegisterFallback(new Cell("Id", DBNull.Value)))
            {
                var manager = this.GetManager(this.Config.Clone());
                manager.Config.ApplyGeneratedKey = true;

                using var query = new Query<Order>(manager);

                Assert.Equal(0, query.Insert(order));
            }
        }

        [Fact]
        public void Select_ByColumnExpressionTR_ShouldSelectColumn()
        {
            // Arrange
            var expected = new SqlExpression("[City]");
            var query = new Query<Address>();
            Expression<ColumnExpression<Address, string>> expression = x => x.City;

            // Act
            var result = query.SelectColumn(expression);

            // Assert
            Assert.NotNull(result);
            QueryAssert.Equal(query, expected, result.Info.Select.FirstOrDefault().ToSafeExpression(query.Info, false));
        }


        [Fact]
        public void QueryToStringShowDeferrerColumn()
        {
            // Arrange
            var expected = "SELECT [City] FROM [Address] WHERE [Name] LIKE ?";
            var query = new Query<Address>();

            // Act
            var result = query.Select(x => x.City).WhereContains(x => x.Name, "Mr.");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.ToString(), query.ToString());
        }

        [Fact]
        public void Should_Success_Query_ToString_With_Invalid_Select_Column_Test()
        {
            var expected = "SELECT ! FROM [Customers] WHERE [Name] LIKE ?";
            var query = new Query<Customer>();

            // Act
            var result = query.Select(x => x.Address!.Id).WhereContains(x => x.Name, "Mr.");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.ToString(), query.ToString());
        }
    }
}

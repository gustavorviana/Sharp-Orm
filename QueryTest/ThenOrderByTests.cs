using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using Xunit.Abstractions;

namespace QueryTest
{
    public class ThenOrderByTests(ITestOutputHelper? output) : DbMockFallbackTest(output)
    {
        [Fact]
        public void ThenOrderBy_ShouldAddAscendingOrderAfterExistingOrderBy()
        {
            // Arrange
            using var query = new Query<Address>();
            query.OrderBy(x => x.City);

            // Act
            var result = query.ThenOrderBy(x => x.Street);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Info.Orders.Length);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.City) && o.Order == OrderBy.Asc);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Street) && o.Order == OrderBy.Asc);
        }

        [Fact]
        public void ThenOrderByDesc_ShouldAddDescendingOrderAfterExistingOrderBy()
        {
            // Arrange
            using var query = new Query<Address>();
            query.OrderBy(x => x.City);

            // Act
            var result = query.ThenOrderByDesc(x => x.Street);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Info.Orders.Length);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.City) && o.Order == OrderBy.Asc);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Street) && o.Order == OrderBy.Desc);
        }

        [Fact]
        public void ThenOrderBy_WithMultipleColumns_ShouldAddAllColumns()
        {
            // Arrange
            using var query = new Query<Address>();
            query.OrderBy(x => x.City);

            // Act
            var result = query.ThenOrderBy(x => new { x.Street, x.Name });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Info.Orders.Length);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.City) && o.Order == OrderBy.Asc);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Street) && o.Order == OrderBy.Asc);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Name) && o.Order == OrderBy.Asc);
        }

        [Fact]
        public void ThenOrderBy_WithoutExistingOrderBy_ShouldStillWork()
        {
            // Arrange
            using var query = new Query<Address>();

            // Act
            var result = query.ThenOrderBy(x => x.Street);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Info.Orders);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Street) && o.Order == OrderBy.Asc);
        }

        [Fact]
        public void ThenOrderBy_ShouldPreserveExistingOrders()
        {
            // Arrange
            using var query = new Query<Address>();
            query.OrderBy(x => x.City);
            query.OrderByDesc(x => x.Name);

            // Act
            var result = query.ThenOrderBy(x => x.Street);

            // Assert
            Assert.NotNull(result);
            // OrderByDesc substitui o OrderBy anterior, então temos apenas Name (Desc) e Street (Asc)
            Assert.Equal(2, result.Info.Orders.Length);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Name) && o.Order == OrderBy.Desc);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Street) && o.Order == OrderBy.Asc);
        }

        [Fact]
        public void ThenOrderBy_WithExpression_ShouldGenerateCorrectSQL()
        {
            // Arrange
            using var query = new Query<Address>();
            query.OrderBy(x => x.City);

            // Act
            var result = query.ThenOrderBy(x => x.Street);
            var sqlExpression = result.Grammar().Select();
            var sql = sqlExpression.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("ORDER BY", sql);
            Assert.Contains("[City]", sql);
            Assert.Contains("[Street]", sql);
        }

        [Fact]
        public void ThenOrderBy_WithOrderByParameter_ShouldRespectOrderDirection()
        {
            // Arrange
            using var query = new Query<Address>();
            query.OrderBy(x => x.City);

            // Act
            var result = query.ThenOrderBy(OrderBy.Desc, x => x.Street);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Info.Orders.Length);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.City) && o.Order == OrderBy.Asc);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Street) && o.Order == OrderBy.Desc);
        }

        [Fact]
        public void ThenOrderBy_CanChainMultipleCalls()
        {
            // Arrange
            using var query = new Query<Address>();
            query.OrderBy(x => x.City);

            // Act
            var result = query.ThenOrderBy(x => x.Street)
                             .ThenOrderByDesc(x => x.Name);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Info.Orders.Length);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.City) && o.Order == OrderBy.Asc);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Street) && o.Order == OrderBy.Asc);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.Name) && o.Order == OrderBy.Desc);
        }

        [Fact]
        public void ThenOrderBy_WithComplexExpression_ShouldWork()
        {
            // Arrange
            using var query = new Query<Address>();
            query.OrderBy(x => x.City);

            // Act
            var result = query.ThenOrderBy(x => x.Street.Substring(0, 5));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Info.Orders.Length);
            Assert.Contains(result.Info.Orders, o => o.Column.Name == nameof(Address.City) && o.Order == OrderBy.Asc);
            // Verifica que a expressão complexa foi processada
            var streetOrder = result.Info.Orders.FirstOrDefault(o => o.Column.Name == nameof(Address.Street));
            Assert.NotNull(streetOrder);
            Assert.Equal(OrderBy.Asc, streetOrder.Order);
        }
    }
}


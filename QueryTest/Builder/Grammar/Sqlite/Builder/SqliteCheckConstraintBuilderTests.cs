using SharpOrm.Builder.Grammars.Sqlite.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Sqlite.Builder
{
    public class SqliteCheckConstraintBuilderTests
    {
        private readonly SqliteCheckConstraintBuilder _builder;

        public SqliteCheckConstraintBuilderTests()
        {
            _builder = new SqliteCheckConstraintBuilder();
        }

        [Fact]
        public void CanWork_WithCheckConstraint_ReturnsTrue()
        {
            // Act
            var result = _builder.CanWork(typeof(CheckConstraint));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_SimpleCheck_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new CheckConstraint("Products", "Price > 0", "CHK_Products_Price");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT \"CHK_Products_Price\" CHECK (Price > 0)", result.ToString());
        }

        [Fact]
        public void Build_WithSqliteDateFunction_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new CheckConstraint("Orders", "datetime(OrderDate) <= datetime('now')", "CHK_Orders_Date");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT \"CHK_Orders_Date\" CHECK (datetime(OrderDate) <= datetime('now'))", result.ToString());
        }

        [Fact]
        public void Build_WithComplexExpression_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new CheckConstraint("Users", "Age >= 18 AND Age <= 120 AND Status = 'Active'", "CHK_Users_Complex");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT \"CHK_Users_Complex\" CHECK (Age >= 18 AND Age <= 120 AND Status = 'Active')", result.ToString());
        }
    }
}

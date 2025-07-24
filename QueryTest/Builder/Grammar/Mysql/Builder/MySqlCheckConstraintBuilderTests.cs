using SharpOrm.Builder.Grammars.Mysql.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Mysql.Builder
{
    public class MySqlCheckConstraintBuilderTests
    {
        private readonly MySqlCheckConstraintBuilder _builder;

        public MySqlCheckConstraintBuilderTests()
        {
            _builder = new MySqlCheckConstraintBuilder();
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
            Assert.Equal("CONSTRAINT `CHK_Products_Price` CHECK (Price > 0)", result.ToString());
        }

        [Fact]
        public void Build_ComplexCheck_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new CheckConstraint("Orders", "Status IN ('Pending', 'Processing', 'Shipped')", "CHK_Orders_Status");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT `CHK_Orders_Status` CHECK (Status IN ('Pending', 'Processing', 'Shipped'))", result.ToString());
        }
    }
}

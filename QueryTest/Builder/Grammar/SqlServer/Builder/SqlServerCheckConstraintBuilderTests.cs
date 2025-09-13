using SharpOrm.Builder.Grammars.SqlServer.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.SqlServer.Builder
{
    public class SqlServerCheckConstraintBuilderTests
    {
        private readonly SqlServerCheckConstraintBuilder _builder;

        public SqlServerCheckConstraintBuilderTests()
        {
            _builder = new SqlServerCheckConstraintBuilder();
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
            Assert.Equal("CONSTRAINT [CHK_Products_Price] CHECK (Price > 0)", result.ToString());
        }

        [Fact]
        public void Build_ComplexCheck_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new CheckConstraint("Users", "Age >= 18 AND Age <= 120", "CHK_Users_Age");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT [CHK_Users_Age] CHECK (Age >= 18 AND Age <= 120)", result.ToString());
        }

        [Fact]
        public void Build_WithoutConstraintName_UsesDefaultName()
        {
            // Arrange
            var constraint = new CheckConstraint("Products", "Price > 0");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.StartsWith("CONSTRAINT [CHK_Products_", result.ToString());
            Assert.EndsWith("] CHECK (Price > 0)", result.ToString());
        }
    }
}

using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Table.Constraints
{
    public class CheckConstraintTests
    {
        [Fact]
        public void CheckConstraint_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var tableName = "Products";
            var expression = "Price > 0";
            var constraintName = "CHK_Products_Price";

            // Act
            var constraint = new CheckConstraint(tableName, expression, constraintName);

            // Assert
            Assert.Equal(tableName, constraint.Table);
            Assert.Equal(expression, constraint.Expression);
            Assert.Equal(constraintName, constraint.Name);
        }

        [Fact]
        public void CheckConstraint_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var constraint = new CheckConstraint("Products", "Price > 0", "CHK_Products_Price");

            // Act
            var result = constraint.ToString();

            // Assert
            Assert.Equal("CHECK (Price > 0)", result);
        }

        [Fact]
        public void CheckConstraint_GetDefaultName_ReturnsCorrectFormat()
        {
            // Arrange
            var expression = "Age >= 18 AND Age <= 120";
            var constraint = new CheckConstraint("Users", expression);

            // Act
            var defaultName = constraint.GetDefaultName();

            // Assert
            Assert.StartsWith("CHK_Users_", defaultName);
            Assert.True(defaultName.Length > "CHK_Users_".Length);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CheckConstraint_Constructor_WithInvalidExpression_ThrowsArgumentException(string expression)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new CheckConstraint("Users", expression, "CHK_Test"));
        }
    }
}

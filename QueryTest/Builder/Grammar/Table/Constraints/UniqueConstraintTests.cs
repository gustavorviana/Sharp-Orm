using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Table.Constraints
{
    public class UniqueConstraintTests
    {
        [Fact]
        public void UniqueConstraint_SingleColumn_SetsPropertiesCorrectly()
        {
            // Arrange
            var tableName = "Users";
            var column = "Email";
            var constraintName = "UC_Users_Email";

            // Act
            var constraint = new UniqueConstraint(tableName, column, constraintName);

            // Assert
            Assert.Equal(tableName, constraint.Table);
            Assert.Equal(constraintName, constraint.Name);
            Assert.Equal([column], constraint.Columns);
            Assert.False(constraint.IsComposite);
        }

        [Fact]
        public void UniqueConstraint_MultipleColumns_SetsPropertiesCorrectly()
        {
            // Arrange
            var tableName = "Orders";
            var columns = new[] { "CustomerId", "OrderNumber" };
            var constraintName = "UC_Orders_CustomerOrder";

            // Act
            var constraint = new UniqueConstraint(tableName, columns, constraintName);

            // Assert
            Assert.Equal(tableName, constraint.Table);
            Assert.Equal(constraintName, constraint.Name);
            Assert.Equal(columns, constraint.Columns);
            Assert.True(constraint.IsComposite);
        }

        [Fact]
        public void UniqueConstraint_GetDefaultName_ReturnsCorrectFormat()
        {
            // Arrange
            var constraint = new UniqueConstraint("Users", ["FirstName", "LastName"]);

            // Act
            var defaultName = constraint.GetDefaultName();

            // Assert
            Assert.Equal("UC_Users_FirstName_LastName", defaultName);
        }

        [Fact]
        public void UniqueConstraint_Constructor_WithNullColumns_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new UniqueConstraint("Users", (string[])null, "UC_Users"));
        }

        [Fact]
        public void UniqueConstraint_Constructor_WithEmptyColumns_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new UniqueConstraint("Users", [], "UC_Users"));
        }
    }
}
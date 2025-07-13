using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Table.Constraints
{
    public class PrimaryKeyConstraintTests
    {
        [Fact]
        public void PrimaryKeyConstraint_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var tableName = "Users";
            var columns = new string[] { "Id" };
            var constraintName = "PK_Users_Id";

            // Act
            var constraint = new PrimaryKeyConstraint(tableName, columns, constraintName);

            // Assert
            Assert.Equal(tableName, constraint.Table);
            Assert.Equal(constraintName, constraint.Name);
            Assert.Equal(columns, constraint.Columns);
        }

        [Fact]
        public void PrimaryKeyConstraint_GetDefaultName_ReturnsCorrectFormat()
        {
            // Arrange
            var tableName = "Users";
            var columns = new[] { "Id", "CompanyId" };
            var constraint = new PrimaryKeyConstraint(tableName, columns);

            // Act
            var defaultName = constraint.GetDefaultName();

            // Assert
            Assert.Equal("PK_Users", defaultName);
        }

        [Fact]
        public void PrimaryKeyConstraint_Equals_ReturnsTrueForSameConstraints()
        {
            // Arrange
            var constraint1 = new PrimaryKeyConstraint("Users", ["Id"], "PK_Users");
            var constraint2 = new PrimaryKeyConstraint("Users", ["Id"], "PK_Users");

            // Act & Assert
            Assert.True(constraint1.Equals(constraint2));
            Assert.Equal(constraint1.GetHashCode(), constraint2.GetHashCode());
            Assert.True(constraint1 == constraint2);
        }

        [Fact]
        public void PrimaryKeyConstraint_Equals_ReturnsFalseForDifferentConstraints()
        {
            // Arrange
            var constraint1 = new PrimaryKeyConstraint("Users", ["Id"], "PK_Users");
            var constraint2 = new PrimaryKeyConstraint("Orders", ["Id"], "PK_Orders");

            // Act & Assert
            Assert.False(constraint1.Equals(constraint2));
            Assert.True(constraint1 != constraint2);
        }

        [Fact]
        public void PrimaryKeyConstraint_Constructor_WithNullColumns_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PrimaryKeyConstraint("Users", null, "PK_Users"));
        }

        [Fact]
        public void PrimaryKeyConstraint_Constructor_WithEmptyColumns_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new PrimaryKeyConstraint("Users", [], "PK_Users"));
        }
    }
}

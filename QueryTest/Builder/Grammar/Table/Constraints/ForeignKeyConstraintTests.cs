using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Table.Constraints
{
    public class ForeignKeyConstraintTests
    {
        [Fact]
        public void ForeignKeyConstraint_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var foreignTable = "Orders";
            var foreignKeyColumn = "CustomerId";
            var referencedTable = "Customers";
            var referencedColumn = "Id";
            var constraintName = "FK_Orders_Customers";

            // Act
            var constraint = new ForeignKeyConstraint(foreignTable, foreignKeyColumn, referencedTable, referencedColumn, constraintName);

            // Assert
            Assert.Equal(foreignTable, constraint.ForeignTable);
            Assert.Equal(foreignKeyColumn, constraint.ForeignKeyColumn);
            Assert.Equal(referencedTable, constraint.ReferencedTable);
            Assert.Equal(referencedColumn, constraint.ReferencedColumn);
            Assert.Equal(constraintName, constraint.ConstraintName);
        }

        [Fact]
        public void ForeignKeyConstraint_GetDefaultName_ReturnsCorrectFormat()
        {
            // Arrange
            var constraint = new ForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id");

            // Act
            var defaultName = constraint.GetDefaultName();

            // Assert
            Assert.Equal("FK_Customers_CustomerId", defaultName);
        }

        [Fact]
        public void ForeignKeyConstraint_WithCascadeRules_SetsRulesCorrectly()
        {
            // Arrange
            var constraint = new ForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id")
            {
                OnDelete = DbRule.Cascade,
                OnUpdate = DbRule.SetNull
            };

            // Act & Assert
            Assert.Equal(DbRule.Cascade, constraint.OnDelete);
            Assert.Equal(DbRule.SetNull, constraint.OnUpdate);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ForeignKeyConstraint_Constructor_WithInvalidForeignTable_ThrowsArgumentException(string foreignTable)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ForeignKeyConstraint(foreignTable, "CustomerId", "Customers", "Id"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ForeignKeyConstraint_Constructor_WithInvalidForeignKeyColumn_ThrowsArgumentException(string foreignKeyColumn)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ForeignKeyConstraint("Orders", foreignKeyColumn, "Customers", "Id"));
        }
    }
}

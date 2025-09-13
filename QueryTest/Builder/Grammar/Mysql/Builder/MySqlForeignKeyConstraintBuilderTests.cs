using SharpOrm.Builder.Grammars.Mysql.Builder;
using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Mysql.Builder
{
    public class MysqlForeignKeyConstraintBuilderTests
    {
        private readonly MySqlForeignKeyConstraintBuilder _builder;

        public MysqlForeignKeyConstraintBuilderTests()
        {
            _builder = new MySqlForeignKeyConstraintBuilder();
        }

        [Fact]
        public void CanWork_WithForeignKeyConstraint_ReturnsTrue()
        {
            // Act
            var result = _builder.CanWork(typeof(ForeignKeyConstraint));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_BasicForeignKey_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new ForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id", "FK_Orders_Customers");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT `FK_Orders_Customers` FOREIGN KEY (`CustomerId`) REFERENCES `Customers` (`Id`)", result.ToString());
        }

        [Fact]
        public void Build_WithCascadeDeleteAndSetNullUpdate_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new ForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id", "FK_Orders_Customers")
            {
                OnDelete = DbRule.Cascade,
                OnUpdate = DbRule.SetNull
            };

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT `FK_Orders_Customers` FOREIGN KEY (`CustomerId`) REFERENCES `Customers` (`Id`) ON DELETE CASCADE ON UPDATE SET NULL", result.ToString());
        }
    }
}

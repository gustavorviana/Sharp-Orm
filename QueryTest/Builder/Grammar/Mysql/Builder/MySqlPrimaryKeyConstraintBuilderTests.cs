using SharpOrm.Builder.Grammars.Mysql.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Mysql.Builder
{
    public class MysqlPrimaryKeyConstraintBuilderTests
    {
        private readonly MySqlPrimaryKeyConstraintBuilder _builder;

        public MysqlPrimaryKeyConstraintBuilderTests()
        {
            _builder = new MySqlPrimaryKeyConstraintBuilder();
        }

        [Fact]
        public void CanWork_WithPrimaryKeyConstraint_ReturnsTrue()
        {
            // Act
            var result = _builder.CanWork(typeof(PrimaryKeyConstraint));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_SingleColumn_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new PrimaryKeyConstraint("Users", new[] { "Id" }, "PK_Users");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)", result.ToString());
        }

        [Fact]
        public void Build_MultipleColumns_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new PrimaryKeyConstraint("OrderItems", new[] { "OrderId", "ProductId" });

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT `PK_OrderItems_OrderId_ProductId` PRIMARY KEY (`OrderId`,`ProductId`)", result.ToString());
        }
    }
}

using SharpOrm.Builder.Grammars.SqlServer.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.SqlServer.Builder
{
    public class SqlServerPrimaryKeyConstraintBuilderTests
    {
        private readonly SqlServerPrimaryKeyConstraintBuilder _builder;

        public SqlServerPrimaryKeyConstraintBuilderTests()
        {
            _builder = new SqlServerPrimaryKeyConstraintBuilder();
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
        public void CanWork_WithOtherConstraintType_ReturnsFalse()
        {
            // Act
            var result = _builder.CanWork(typeof(ForeignKeyConstraint));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Build_WithSingleColumn_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new PrimaryKeyConstraint("Users", ["Id"], "PK_Users");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT [PK_Users] PRIMARY KEY ([Id])", result.ToString());
        }

        [Fact]
        public void Build_WithMultipleColumns_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new PrimaryKeyConstraint("OrderItems", ["OrderId", "ProductId"], "PK_OrderItems");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT [PK_OrderItems] PRIMARY KEY ([OrderId],[ProductId])", result.ToString());
        }

        [Fact]
        public void Build_WithoutConstraintName_UsesDefaultName()
        {
            // Arrange
            var constraint = new PrimaryKeyConstraint("Users", ["Id"]);

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT [PK_Users_Id] PRIMARY KEY ([Id])", result.ToString());
        }
    }
}

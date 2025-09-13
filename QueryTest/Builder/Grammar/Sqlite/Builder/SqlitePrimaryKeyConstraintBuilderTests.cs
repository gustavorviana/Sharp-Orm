using SharpOrm.Builder.Grammars.Sqlite.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Sqlite.Builder
{
    public class SqlitePrimaryKeyConstraintBuilderTests
    {
        private readonly SqlitePrimaryKeyConstraintBuilder _builder;

        public SqlitePrimaryKeyConstraintBuilderTests()
        {
            _builder = new SqlitePrimaryKeyConstraintBuilder();
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
        public void Build_SingleColumn_AutoIncrement_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new PrimaryKeyConstraint("Users", ["Id"], "PK_Users")
            {
                AutoIncrement = true
            };

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("PRIMARY KEY AUTOINCREMENT", result.ToString());
        }

        [Fact]
        public void Build_SingleColumn_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new PrimaryKeyConstraint("Users", ["Id"], "PK_Users");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("PRIMARY KEY", result.ToString());
        }

        [Fact]
        public void Build_MultipleColumns_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new PrimaryKeyConstraint("OrderItems", ["OrderId", "ProductId"]);

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("PRIMARY KEY (\"OrderId\",\"ProductId\")", result.ToString());
        }
    }
}

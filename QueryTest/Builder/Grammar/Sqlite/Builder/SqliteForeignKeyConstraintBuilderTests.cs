using SharpOrm.Builder.Grammars.Sqlite.Builder;
using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Sqlite.Builder
{
    public class SqliteForeignKeyConstraintBuilderTests
    {
        private readonly SqliteForeignKeyConstraintBuilder _builder;

        public SqliteForeignKeyConstraintBuilderTests()
        {
            _builder = new SqliteForeignKeyConstraintBuilder();
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
            Assert.Equal("CONSTRAINT \"FK_Orders_Customers\" FOREIGN KEY (\"CustomerId\") REFERENCES \"Customers\" (\"Id\")", result.ToString());
        }

        [Fact]
        public void Build_WithCascadeRules_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new ForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id", "FK_Orders_Customers")
            {
                OnDelete = DbRule.Cascade,
                OnUpdate = DbRule.SetDefault
            };

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT \"FK_Orders_Customers\" FOREIGN KEY (\"CustomerId\") REFERENCES \"Customers\" (\"Id\") ON DELETE CASCADE ON UPDATE SET DEFAULT", result.ToString());
        }

        [Fact]
        public void Build_WithNoAction_DoesNotIncludeRule()
        {
            // Arrange
            var constraint = new ForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id", "FK_Orders_Customers")
            {
                OnDelete = DbRule.None,
                OnUpdate = DbRule.None
            };

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT \"FK_Orders_Customers\" FOREIGN KEY (\"CustomerId\") REFERENCES \"Customers\" (\"Id\")", result.ToString());
        }
    }
}

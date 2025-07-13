using SharpOrm.Builder.Grammars.SqlServer.Builder;
using SharpOrm.Builder.Grammars.Table;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.SqlServer.Builder
{
    public class SqlServerForeignKeyConstraintBuilderTests
    {
        private readonly SqlServerForeignKeyConstraintBuilder _builder;

        public SqlServerForeignKeyConstraintBuilderTests()
        {
            _builder = new SqlServerForeignKeyConstraintBuilder();
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
            Assert.Equal("CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id])", result.ToString());
        }

        [Fact]
        public void Build_WithCascadeDelete_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new ForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id", "FK_Orders_Customers")
            {
                OnDelete = DbRule.Cascade
            };

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE", result.ToString());
        }

        [Fact]
        public void Build_WithSetNullUpdate_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new ForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id", "FK_Orders_Customers")
            {
                OnUpdate = DbRule.SetNull
            };

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON UPDATE SET NULL", result.ToString());
        }

        [Fact]
        public void Build_WithBothDeleteAndUpdate_GeneratesCorrectSql()
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
            Assert.Equal("CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE ON UPDATE SET DEFAULT", result.ToString());
        }

        [Fact]
        public void Build_WithoutConstraintName_UsesDefaultName()
        {
            // Arrange
            var constraint = new ForeignKeyConstraint("Orders", "CustomerId", "Customers", "Id");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Contains("CONSTRAINT [FK_Customers_CustomerId]", result.ToString());
        }
    }
}
